using Microsoft.AspNetCore.Mvc;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using SchoolProject.Service;
using MailKit;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace SchoolProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly TwoFactorAuthService _twoFactorService;


        public AccountController(ApplicationDbContext context, EmailService emailService, TwoFactorAuthService twoFactorService)
        {
            _context = context;
            _emailService = emailService;
            _twoFactorService = twoFactorService;
        }



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Accounts
                .FirstOrDefault(a =>
                    (a.Email.ToLower() == model.Email.ToLower() || a.Name.ToLower() == model.Email.ToLower())
                    && a.UserStatus == UserStatus.Active);

            // Replace BCrypt verification with simple comparison
            if (user == null || user.Password != model.Password) // Insecure - see better alternative below
            {
                TempData["ErrorMessage"] = "Invalid login attempt or account is inactive.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Login successful!";
            TempData["SuccessMessage"] = user.Name;

                 var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("UserID", user.UserID.ToString())
                };


            if (user.IsTwoFactorEnabled)
            {
                // Store user ID in session for 2FA verification
                HttpContext.Session.SetInt32("TwoFactorUserId", user.UserID);
                return RedirectToAction("TwoFactorLogin");
            }



            var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            return user.Role switch
            {
                UserRole.Administrator => RedirectToAction("Dashboard", "Admin"),
                UserRole.Lecturer => RedirectToAction("Dashboard", "Lecturer"),
                UserRole.Student => RedirectToAction("Dashboard", "Student"),
                _ => RedirectToAction("Index", "Home"),
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");

            // Add a success message to TempData
            TempData["SuccessMessage"] = "You have successfully logged out.";

            // Optionally, clear TempData for any errors
            TempData.Clear();

            return RedirectToAction("Login", "Account");
        }



        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }




        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                // Generate a simple 6-digit PIN (no hashing)
                string resetPin = new Random().Next(100000, 999999).ToString();

                // Prepare placeholders for dynamic content
                var placeholders = new Dictionary<string, string>
        {
            { "Name", user.Name },
            { "Email", email },
            { "ResetPin", resetPin }
        };

                try
                {
                    // Send email using the template
                    var subject = "Password Reset Request - SchoolProject";
                    await _emailService.SendEmailWithTemplateAsync(email, subject, "password-reset-template.html", placeholders);

                    // Save the plain PIN and expiration time in the database
                    user.ResetPin = resetPin;
                    user.ResetPinExpiration = DateTime.Now.AddMinutes(5);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "A password reset PIN has been sent to your email address.";
                    return View("ResetPassword");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred while sending the reset email. Please try again.";
                    return View();
                }
            }

            TempData["ErrorMessage"] = "The email address you entered is not associated with any account.";
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string pin, string newPassword)
        {
            // Validate the PIN: it should be 6 digits long
            if (pin.Length != 6 || !pin.All(char.IsDigit))
            {
                ModelState.AddModelError(string.Empty, "PIN must be a 6-digit code.");
                return View();
            }

            // Find user with matching unexpired PIN
            var user = _context.Accounts.FirstOrDefault(a =>
                a.ResetPin == pin &&
                a.ResetPinExpiration > DateTime.Now);

            if (user != null)
            {
                // Store the new password in plain text (no hashing)
                user.Password = newPassword;

                // Clear the reset PIN
                user.ResetPin = null;

                // Save changes
                _context.SaveChanges();

                // Redirect to login page
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, "Invalid PIN or PIN has expired.");
            return View();
        }



        [Authorize]
        [HttpGet]
        public IActionResult EditProfile()
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Find user in database
            var user = _context.Accounts.FirstOrDefault(u => u.UserID == userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Map to ViewModel
            var viewModel = new EditProfileViewModel
            {
                Name = user.Name,
                Surname = user.Surname,
                Title = user.Title,
                Email = user.Email
            };

            return View(viewModel);
        }



        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst("UserID");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                // Find user in database
                var user = await _context.Accounts.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                // Check if email is already taken by another user
                var existingUser = await _context.Accounts
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UserID != userId);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email address is already in use.");
                    return View(model);
                }

                // Update user properties
                user.Name = model.Name;
                user.Surname = model.Surname;
                user.Title = model.Title;
                user.Email = model.Email;

                // Save changes
                await _context.SaveChangesAsync();

                // Update the Name claim if needed
                var identity = User.Identity as ClaimsIdentity;
                var nameClaim = identity?.FindFirst(ClaimTypes.Name);
                if (nameClaim != null)
                {
                    identity.RemoveClaim(nameClaim);
                    identity.AddClaim(new Claim(ClaimTypes.Name, $"{model.Name} {model.Surname}"));

                    // Re-sign in to update the cookie
                    await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("EditProfile");
            }
            catch (Exception ex)
            {
                // Log the exception (you should implement proper logging)
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again.";
                return View(model);
            }
        }



        [Authorize]
        [HttpGet]
        public IActionResult ViewProfile()
        {
            // Get user ID from claims instead of name
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            var user = _context.Accounts.FirstOrDefault(a => a.UserID == userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new ViewProfileViewModel
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                Title = user.Title,
                Role = user.Role.ToString(),
                UserStatus = user.UserStatus.ToString()
            };

            return View(model);
        }


        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get user ID from claims instead of name
                var userIdClaim = User.FindFirst("UserID");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                var user = _context.Accounts.FirstOrDefault(a => a.UserID == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                // Check if the current password is correct (plain text comparison)
                if (model.CurrentPassword != user.Password)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View(model);
                }

                // Ensure that the new password is not the same as the current password
                if (model.CurrentPassword == model.NewPassword)
                {
                    ModelState.AddModelError("NewPassword", "The new password cannot be the same as the current password.");
                    return View(model);
                }

                // Ensure new password and confirmation match
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "The new password and confirmation password do not match.");
                    return View(model);
                }

                // Update the password (store in plain text)
                user.Password = model.NewPassword;
                await _context.SaveChangesAsync();

                // Prepare dynamic content for email template
                var placeholders = new Dictionary<string, string>
        {
            { "Name", user.Name },
            { "ChangeDate", DateTime.Now.ToString("dddd, MMMM dd, yyyy hh:mm tt") }
        };

                // Send the security email using template
                await _emailService.SendEmailWithTemplateAsync(
                    user.Email,
                    "Security Alert: Your Password Was Changed",
                    "PasswordChangeTemplate.html", // Template file
                    placeholders
                );

                TempData["SuccessMessage"] = "Password changed successfully!";

                return user.Role switch
                {
                    UserRole.Administrator => RedirectToAction("Dashboard", "Admin"),
                    UserRole.Lecturer => RedirectToAction("Dashboard", "Lecturer"),
                    UserRole.Student => RedirectToAction("Dashboard", "Student"),
                    _ => RedirectToAction("Index", "Home"),
                };
            }
            catch (Exception ex)
            {
                // Log the exception here
                TempData["ErrorMessage"] = "An error occurred while changing your password. Please try again.";
                return View(model);
            }
        }




        [Authorize]
        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View(); // Show confirmation page
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed()
        {
            var userName = User.Identity.Name;
            var user = await _context.Accounts.FirstOrDefaultAsync(a => a.Name == userName);
            if (user == null)
                return NotFound();

            user.UserStatus = UserStatus.Inactive;
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync("MyCookieAuth");
            TempData["SuccessMessage"] = "Your account has been deactivated.";

            return RedirectToAction("Login", "Account");
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public IActionResult ReactivateAccount()
        {
            var users = _context.Accounts
                .Where(u => u.UserStatus == UserStatus.Inactive)
                .ToList();

            return View(users);
        }



        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateAccount(int id)
        {
            var user = await _context.Accounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.UserStatus = UserStatus.Active;
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User {user.Name} has been reactivated.";
            return RedirectToAction("ManageUsers"); // Adjust this if your user listing action is named differently
        }




        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DownloadProfile()
        {
            // Get user ID from claims instead of name
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            var user = await _context.Accounts.FirstOrDefaultAsync(a => a.UserID == userId);

            if (user == null)
            {
                return NotFound();
            }

            var userProfile = _context.Accounts
                .Where(u => u.UserID == userId)
                .Select(u => new ViewProfileViewModel
                {
                    Title = u.Title,
                    Name = u.Name,
                    Surname = u.Surname,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    UserStatus = u.UserStatus.ToString()
                }).FirstOrDefault();

            if (userProfile == null)
            {
                return NotFound();
            }

            var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 50, 50, 50, 50);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            writer.CloseStream = false;

            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Blue);
            var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

            document.Add(new Paragraph("User Profile", titleFont));
            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph($"Title: {userProfile.Title}", textFont));
            document.Add(new Paragraph($"Name: {userProfile.Name}", textFont));
            document.Add(new Paragraph($"Surname: {userProfile.Surname}", textFont));
            document.Add(new Paragraph($"Email: {userProfile.Email}", textFont));
            document.Add(new Paragraph($"Role: {userProfile.Role}", textFont));
            document.Add(new Paragraph($"Status: {userProfile.UserStatus}", textFont));

            document.Close();
            memoryStream.Position = 0;

            // Generate dynamic filename (e.g., "Profile_JohnDoe_20240414.pdf")
            var fileName = $"Profile_{user.Name}_{user.Surname}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(memoryStream, "application/pdf", fileName);
        }













        [HttpGet]
        public IActionResult TwoFactorLogin()
        {
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            return View(new TwoFactorLoginViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorLogin(TwoFactorLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Accounts.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            bool isValidCode = false;

            if (model.UseRecoveryCode && !string.IsNullOrEmpty(model.RecoveryCode))
            {
                // Validate recovery code
                var recoveryCodes = user.TwoFactorRecoveryCodes?.Split(',').ToList() ?? new List<string>();
                if (recoveryCodes.Contains(model.RecoveryCode.ToUpper()))
                {
                    // Remove used recovery code
                    recoveryCodes.Remove(model.RecoveryCode.ToUpper());
                    user.TwoFactorRecoveryCodes = string.Join(",", recoveryCodes);
                    await _context.SaveChangesAsync();
                    isValidCode = true;
                }
            }
            else if (!string.IsNullOrEmpty(model.VerificationCode))
            {
                // Validate TOTP code
                isValidCode = _twoFactorService.ValidatePin(user.TwoFactorSecretKey, model.VerificationCode);
            }

            if (!isValidCode)
            {
                ModelState.AddModelError("", "Invalid verification code.");
                return View(model);
            }

            // Clear the session
            HttpContext.Session.Remove("TwoFactorUserId");

            // Sign in the user
            await SignInUser(user, false);
            return RedirectBasedOnRole(user.Role);
        }


















        // Setup Two Factor Authentication GET
        [Authorize]
        [HttpGet]
        public IActionResult SetupTwoFactor()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = _context.Accounts.FirstOrDefault(u => u.UserID == userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (user.IsTwoFactorEnabled)
            {
                TempData["ErrorMessage"] = "Two-factor authentication is already enabled.";
                return RedirectToAction("ViewProfile");
            }

            // Generate new secret key
            var secretKey = _twoFactorService.GenerateSecretKey();
            var setupCode = _twoFactorService.GenerateQrCode(user.Email, secretKey);

            var viewModel = new TwoFactorSetupViewModel
            {
                QrCodeImageUrl = setupCode.QrCodeSetupImageUrl,
                ManualEntryKey = secretKey
            };

            // Store secret key in session temporarily
            HttpContext.Session.SetString("TempSecretKey", secretKey);

            return View(viewModel);
        }

        // Setup Two Factor Authentication POST
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupTwoFactor(TwoFactorSetupViewModel model)
        {
            if (string.IsNullOrEmpty(model.VerificationCode))
            {
                ModelState.AddModelError("VerificationCode", "Please enter the verification code from your authenticator app.");
                return View(model);
            }

            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Accounts.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var secretKey = HttpContext.Session.GetString("TempSecretKey");
            if (string.IsNullOrEmpty(secretKey))
            {
                TempData["ErrorMessage"] = "Session expired. Please try again.";
                return RedirectToAction("SetupTwoFactor");
            }

            // Validate the verification code
            if (!_twoFactorService.ValidatePin(secretKey, model.VerificationCode))
            {
                ModelState.AddModelError("VerificationCode", "Invalid verification code. Please try again.");

                // Regenerate QR code for the view
                var setupCode = _twoFactorService.GenerateQrCode(user.Email, secretKey);
                model.QrCodeImageUrl = setupCode.QrCodeSetupImageUrl;
                model.ManualEntryKey = secretKey;

                return View(model);
            }

            // Generate recovery codes
            var recoveryCodes = _twoFactorService.GenerateRecoveryCodes();

            // Save 2FA settings
            user.IsTwoFactorEnabled = true;
            user.TwoFactorSecretKey = secretKey;
            user.TwoFactorRecoveryCodes = string.Join(",", recoveryCodes);

            await _context.SaveChangesAsync();

            // Clear temp session data
            HttpContext.Session.Remove("TempSecretKey");

            // Show recovery codes
            model.RecoveryCodes = recoveryCodes;
            TempData["SuccessMessage"] = "Two-factor authentication has been enabled successfully!";

            return View("TwoFactorRecoveryCodes", model);
        }

        // Disable Two Factor Authentication GET
        [Authorize]
        [HttpGet]
        public IActionResult DisableTwoFactor()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = _context.Accounts.FirstOrDefault(u => u.UserID == userId);
            if (user == null || !user.IsTwoFactorEnabled)
            {
                return RedirectToAction("ViewProfile");
            }

            return View(new DisableTwoFactorViewModel());
        }

        // Disable Two Factor Authentication POST
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Accounts.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Verify current password
            if (user.Password != model.CurrentPassword)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            // Verify 2FA code
            if (!_twoFactorService.ValidatePin(user.TwoFactorSecretKey, model.VerificationCode))
            {
                ModelState.AddModelError("VerificationCode", "Invalid verification code.");
                return View(model);
            }

            // Disable 2FA
            user.IsTwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.TwoFactorRecoveryCodes = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Two-factor authentication has been disabled.";
            return RedirectToAction("ViewProfile");
        }

        // Helper methods
        private async Task SignInUser(Account user, bool isPersistent)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("UserID", user.UserID.ToString())
    };

            var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent
            };

            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        private IActionResult RedirectBasedOnRole(UserRole role)
        {
            return role switch
            {
                UserRole.Administrator => RedirectToAction("Dashboard", "Admin"),
                UserRole.Lecturer => RedirectToAction("Dashboard", "Lecturer"),
                UserRole.Student => RedirectToAction("Dashboard", "Student"),
                _ => RedirectToAction("Index", "Home"),
            };
        }








    }
}

