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
using System.Security.Cryptography;
using System.Text;

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

            // Changed to async
            var user = await _context.Accounts
                .FirstOrDefaultAsync(a =>
                    (a.Email.ToLower() == model.Email.ToLower() || a.Name.ToLower() == model.Email.ToLower())
                    && a.UserStatus == UserStatus.Active);

            if (user == null || user.Password != model.Password)
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
                HttpContext.Session.SetInt32("TwoFactorUserId", user.UserID);
                return RedirectToAction("VerificationCodeLogin");
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
            TempData["SuccessMessage"] = "You have successfully logged out.";
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
                string resetPin = new Random().Next(100000, 999999).ToString();
                var placeholders = new Dictionary<string, string>
                {
                    { "Name", user.Name },
                    { "Email", email },
                    { "ResetPin", resetPin }
                };

                try
                {
                    var subject = "Password Reset Request - SchoolProject";
                    await _emailService.SendEmailWithTemplateAsync(email, subject, "password-reset-template.html", placeholders);

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
        public async Task<IActionResult> ResetPassword(string pin, string newPassword)
        {
            if (pin.Length != 6 || !pin.All(char.IsDigit))
            {
                ModelState.AddModelError(string.Empty, "PIN must be a 6-digit code.");
                return View();
            }

            // Changed to async
            var user = await _context.Accounts.FirstOrDefaultAsync(a =>
                a.ResetPin == pin &&
                a.ResetPinExpiration > DateTime.Now);

            if (user != null)
            {
                user.Password = newPassword;
                user.ResetPin = null;
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, "Invalid PIN or PIN has expired.");
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Changed to async
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

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
                var userIdClaim = User.FindFirst("UserID");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                var user = await _context.Accounts.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                var existingUser = await _context.Accounts
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UserID != userId);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email address is already in use.");
                    return View(model);
                }

                user.Name = model.Name;
                user.Surname = model.Surname;
                user.Title = model.Title;
                user.Email = model.Email;

                await _context.SaveChangesAsync();

                var identity = User.Identity as ClaimsIdentity;
                var nameClaim = identity?.FindFirst(ClaimTypes.Name);
                if (nameClaim != null)
                {
                    identity.RemoveClaim(nameClaim);
                    identity.AddClaim(new Claim(ClaimTypes.Name, $"{model.Name} {model.Surname}"));
                    await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("EditProfile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again.";
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ViewProfile()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Changed to async
            var user = await _context.Accounts.FirstOrDefaultAsync(a => a.UserID == userId);
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
                UserStatus = user.UserStatus.ToString(),
                IsTwoFactorEnabled = user.IsTwoFactorEnabled
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
                var userIdClaim = User.FindFirst("UserID");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                // Changed to async
                var user = await _context.Accounts.FirstOrDefaultAsync(a => a.UserID == userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                if (model.CurrentPassword != user.Password)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View(model);
                }

                if (model.CurrentPassword == model.NewPassword)
                {
                    ModelState.AddModelError("NewPassword", "The new password cannot be the same as the current password.");
                    return View(model);
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "The new password and confirmation password do not match.");
                    return View(model);
                }

                user.Password = model.NewPassword;
                await _context.SaveChangesAsync();

                var placeholders = new Dictionary<string, string>
                {
                    { "Name", user.Name },
                    { "ChangeDate", DateTime.Now.ToString("dddd, MMMM dd, yyyy hh:mm tt") }
                };

                await _emailService.SendEmailWithTemplateAsync(
                    user.Email,
                    "Security Alert: Your Password Was Changed",
                    "PasswordChangeTemplate.html",
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
                TempData["ErrorMessage"] = "An error occurred while changing your password. Please try again.";
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed()
        {
            var userName = User.Identity.Name;
            // Changed to async
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
        public async Task<IActionResult> ReactivateAccount()
        {
            // Changed to async
            var users = await _context.Accounts
                .Where(u => u.UserStatus == UserStatus.Inactive)
                .ToListAsync();

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
            return RedirectToAction("ManageUsers");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DownloadProfile()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Changed to async
            var user = await _context.Accounts.FirstOrDefaultAsync(a => a.UserID == userId);
            if (user == null)
            {
                return NotFound();
            }

            // Changed to async
            var userProfile = await _context.Accounts
                .Where(u => u.UserID == userId)
                .Select(u => new ViewProfileViewModel
                {
                    Title = u.Title,
                    Name = u.Name,
                    Surname = u.Surname,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    UserStatus = u.UserStatus.ToString()
                }).FirstOrDefaultAsync();

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

            var fileName = $"Profile_{user.Name}_{user.Surname}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(memoryStream, "application/pdf", fileName);
        }

        [HttpGet]
        public IActionResult VerificationCodeLogin()
        {
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            return View(new VerificationCodeLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerificationCodeLogin(VerificationCodeLoginViewModel model)
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

            bool isValidCode = _twoFactorService.ValidatePin(user.TwoFactorSecretKey, model.VerificationCode);

            if (isValidCode)
            {
                HttpContext.Session.Remove("TwoFactorUserId");
                await SignInUser(user, false);
                return RedirectBasedOnRole(user.Role);
            }

            ModelState.AddModelError("", "Invalid verification code.");
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetupTwoFactor()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login");
            }

            // Changed to async
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (user.IsTwoFactorEnabled)
            {
                TempData["ErrorMessage"] = "Two-factor authentication is already enabled.";
                return RedirectToAction("ViewProfile");
            }

            var secretKey = _twoFactorService.GenerateSecretKey();
            var setupCode = _twoFactorService.GenerateQrCode(user.Email, secretKey);

            var viewModel = new TwoFactorSetupViewModel
            {
                QrCodeImageUrl = setupCode.QrCodeSetupImageUrl,
                ManualEntryKey = secretKey
            };

            HttpContext.Session.SetString("TempSecretKey", secretKey);
            return View(viewModel);
        }

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

            if (!_twoFactorService.ValidatePin(secretKey, model.VerificationCode))
            {
                ModelState.AddModelError("VerificationCode", "Invalid verification code. Please try again.");
                var setupCode = _twoFactorService.GenerateQrCode(user.Email, secretKey);
                model.QrCodeImageUrl = setupCode.QrCodeSetupImageUrl;
                model.ManualEntryKey = secretKey;
                return View(model);
            }

            var recoveryCodes = _twoFactorService.GenerateRecoveryCodes();
            user.IsTwoFactorEnabled = true;
            user.TwoFactorSecretKey = secretKey;
            user.TwoFactorRecoveryCodes = string.Join(",", recoveryCodes);

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("TempSecretKey");

            model.RecoveryCodes = recoveryCodes;
            TempData["SuccessMessage"] = "Two-factor authentication has been enabled successfully!";
            return View("TwoFactorRecoveryCodes", model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login");
            }

            // Changed to async
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null || !user.IsTwoFactorEnabled)
            {
                return RedirectToAction("ViewProfile");
            }

            return View(new DisableTwoFactorViewModel());
        }

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

            if (user.Password != model.CurrentPassword)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            if (!_twoFactorService.ValidatePin(user.TwoFactorSecretKey, model.VerificationCode))
            {
                ModelState.AddModelError("VerificationCode", "Invalid verification code.");
                return View(model);
            }

            user.IsTwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.TwoFactorRecoveryCodes = null;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Two-factor authentication has been disabled.";
            return RedirectToAction("ViewProfile");
        }



        [HttpGet]
        public async Task<IActionResult> VerifyEmail(int userId, string token)
        {
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
                return NotFound("User not found");

            if (user.UserStatus == UserStatus.Active)
            {
                TempData["SuccessMessage"] = "Email already verified.";
                return RedirectToAction("Login");
            }

            if (user.EmailVerificationTokenHash != token || user.EmailVerificationTokenExpires < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Invalid or expired verification link.";
                return RedirectToAction("Login");
            }

            // Activate account
            user.UserStatus = UserStatus.Active;
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationTokenExpires = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Email verified successfully! You can now log in.";
            return RedirectToAction("Login");
        }








        [HttpGet]
        public IActionResult ResendVerificationEmail()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Please enter your email.";
                return View();
            }

            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                TempData["ErrorMessage"] = "No account found with this email.";
                return View();
            }

            if (user.UserStatus == UserStatus.Active)
            {
                TempData["SuccessMessage"] = "Your account is already verified. You can log in.";
                return RedirectToAction("Login");
            }

            // Generate new token
            string token = Guid.NewGuid().ToString("N");
            user.EmailVerificationTokenHash = token;
            user.EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            // Build verification link
            var verificationLink = Url.Action(
                "VerifyEmail",
                "Account",
                new { userId = user.UserID, token = token },
                protocol: HttpContext.Request.Scheme
            );

            var placeholders = new Dictionary<string, string>
    {
        { "Name", user.Name },
        { "VerificationLink", verificationLink }
    };

            try
            {
                await _emailService.SendEmailWithTemplateAsync(
                    user.Email,
                    "Email Verification - SchoolProject",
                    "EmailVerificationTemplate.html",
                    placeholders
                );

                TempData["SuccessMessage"] = "A new verification link has been sent to your email.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send email: {ex.Message}";
            }

            return View();
        }





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



   

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return System.Convert.ToBase64String(hash);
        }

        private static string GenerateVerificationToken()
        {
            return Guid.NewGuid().ToString("N"); // 32 chars, no dashes
        }

    }
}