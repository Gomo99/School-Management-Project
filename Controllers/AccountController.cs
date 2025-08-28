using iTextSharp.text;
using iTextSharp.text.pdf;
using MailKit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.Service;
using SchoolProject.ViewModel;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly TwoFactorAuthService _twoFactorService;
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
        

        public AccountController(ApplicationDbContext context, EmailService emailService, TwoFactorAuthService twoFactorService
            )
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

         

            // Fetch user
            var user = await _context.Accounts
                .FirstOrDefaultAsync(a =>
                    (a.Email.ToLower() == model.Email.ToLower() || a.Name.ToLower() == model.Email.ToLower()));

            if (user == null || user.UserStatus != UserStatus.Active)
            {
                TempData["ErrorMessage"] = "Invalid login attempt or account is inactive.";
                return View(model);
            }

            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = $"Account is locked. Try again at {user.LockoutEnd.Value.ToLocalTime():hh:mm tt}.";
                return View(model);
            }

            if (user.Password != model.Password)
            {
                // Increment failed attempts
                user.FailedLoginAttempts++;

                // Lock account if max attempts reached
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                    TempData["ErrorMessage"] = $"Too many failed attempts. Account locked for {LockoutDuration.TotalMinutes} minutes.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid login attempt.";
                }

                await _context.SaveChangesAsync();
                return View(model);
            }

            // Reset failed attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            // Successful login logic (2FA handling, cookie auth)
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("UserID", user.UserID.ToString())
    };

            if (user.IsTwoFactorEnabled)
            {
                if (await IsRememberedDeviceValidAsync(user.UserID))
                {
                    await SignInUser(user, model.RememberMe);
                    return RedirectBasedOnRole(user.Role);
                }

                HttpContext.Session.SetInt32("TwoFactorUserId", user.UserID);
                return RedirectToAction("VerificationCodeLogin");
            }

            var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
            var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };
            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectBasedOnRole(user.Role);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Sign out your cookie auth (this is what keeps the user logged in)
            await HttpContext.SignOutAsync("MyCookieAuth");

            // Clear TempData if needed
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
                await RevokeAllRememberedDevicesAsync(user.UserID);

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
                return RedirectToAction("Login");

            var user = await _context.Accounts.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login");

            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                ModelState.AddModelError("", $"Account is locked until {user.LockoutEnd.Value.ToLocalTime():hh:mm tt}.");
                return View(model);
            }

            bool isValidCode = _twoFactorService.ValidatePin(user.TwoFactorSecretKey, model.VerificationCode);

            if (isValidCode)
            {
                // Reset failed attempts after successful 2FA
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();

                if (model.RememberThisDevice)
                {
                    var deviceName = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await RememberCurrentDeviceAsync(user, deviceName);
                }

                HttpContext.Session.Remove("TwoFactorUserId");
                await SignInUser(user, false);
                return RedirectBasedOnRole(user.Role);
            }

            // Increment failed attempts for invalid 2FA code
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                ModelState.AddModelError("", $"Too many failed attempts. Account locked for {LockoutDuration.TotalMinutes} minutes.");
            }
            else
            {
                ModelState.AddModelError("", "Invalid verification code.");
            }
            await _context.SaveChangesAsync();

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
            await RevokeAllRememberedDevicesAsync(user.UserID);
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



        [Authorize]
        public async Task<IActionResult> ManageDevices()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(); // fallback if claim missing

            var userId = int.Parse(userIdClaim);


            var devices = await _context.RememberedDevices
                .Where(d => d.UserId == userId && !d.Revoked)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(devices); // expects a Razor view with a table of devices
        }
        [Authorize]
        // POST: /Device/RevokeDevice/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeDevice(int id)
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(); // fallback if claim missing

            var userId = int.Parse(userIdClaim);
            var device = await _context.RememberedDevices
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (device == null)
            {
                TempData["ErrorMessage"] = "Device not found.";
                return RedirectToAction(nameof(ViewProfile));
            }

            device.Revoked = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Device revoked successfully.";
            return RedirectToAction(nameof(ViewProfile));
        }


        [Authorize]
        // POST: /Device/RevokeAllDevices
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAllDevices()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(); // fallback if claim missing

            var userId = int.Parse(userIdClaim);


            var devices = await _context.RememberedDevices
                .Where(d => d.UserId == userId && !d.Revoked)
                .ToListAsync();

            foreach (var d in devices)
                d.Revoked = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All remembered devices have been revoked.";
            return RedirectToAction(nameof(ViewProfile));
        }






        [HttpGet]
        public IActionResult RecoveryLogin()
        {
            // Check if the 2FA session is active
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
            {
                // No user in session, redirect to login
                return RedirectToAction("Login");
            }

            return View(new RecoveryLoginViewModel());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecoveryLogin(RecoveryLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
                return RedirectToAction("Login");

            var user = await _context.Accounts.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login");

            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                ModelState.AddModelError("", $"Account is locked until {user.LockoutEnd.Value.ToLocalTime():hh:mm tt}.");
                return View(model);
            }

            var codes = (user.TwoFactorRecoveryCodes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (codes.Contains(model.RecoveryCode))
            {
                // Reset failed attempts after successful recovery login
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;

                var remainingCodes = codes.Where(c => c != model.RecoveryCode);
                user.TwoFactorRecoveryCodes = string.Join(",", remainingCodes);

                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("TwoFactorUserId");
                await SignInUser(user, false);

                TempData["SuccessMessage"] = "Logged in successfully using recovery code.";
                return RedirectBasedOnRole(user.Role);
            }

            // Increment failed attempts for invalid recovery code
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                ModelState.AddModelError("", $"Too many failed attempts. Account locked for {LockoutDuration.TotalMinutes} minutes.");
            }
            else
            {
                ModelState.AddModelError("", "Invalid recovery code. Please try again.");
            }

            await _context.SaveChangesAsync();
            return View(model);
        }




        [Authorize]
        [HttpGet]
        public IActionResult ResendRecoveryCodes()
        {
            // Ensure the 2FA session is active
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendRecoveryCodesPost()
        {
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

            if (!user.IsTwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                TempData["ErrorMessage"] = "Two-factor authentication is not enabled for this account.";
                return RedirectToAction("ViewProfile");
            }

            // Generate new recovery codes
            var recoveryCodes = _twoFactorService.GenerateRecoveryCodes();

            // Save codes in the database
            user.TwoFactorRecoveryCodes = string.Join(",", recoveryCodes);
            await _context.SaveChangesAsync();

            // Prepare email placeholders
            var placeholders = new Dictionary<string, string>
    {
        { "Name", user.Name },
        { "RecoveryCodes", string.Join("<br>", recoveryCodes) }, // HTML line breaks
        { "Date", DateTime.Now.ToString("f") }
    };

            try
            {
                await _emailService.SendEmailWithTemplateAsync(
                    user.Email,
                    "Your New Two-Factor Recovery Codes",
                    "RecoveryCodesTemplate.html", // create this HTML template
                    placeholders
                );

                TempData["SuccessMessage"] = "New recovery codes have been generated and sent to your email. Please check your inbox.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send recovery codes via email: {ex.Message}";
                return View();
            }

            // Optionally show them on the screen too
            return View("TwoFactorRecoveryCodes", new TwoFactorSetupViewModel { RecoveryCodes = recoveryCodes });
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






















        [HttpPost]
        [AllowAnonymous]
        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
        {
            // Read the external authenticate result from Google's handler
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Google authentication failed.";
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Claims;
            // Try multiple claim types for compatibility
            var email = GetFirstClaimValue(claims, ClaimTypes.Email, "email");
            var name = GetFirstClaimValue(claims, ClaimTypes.Name, "name");
            var givenName = GetFirstClaimValue(claims, ClaimTypes.GivenName, "given_name");
            var surname = GetFirstClaimValue(claims, ClaimTypes.Surname, "family_name", "family-name");

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Could not retrieve email from Google.";
                // Clear external cookie and redirect
                await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }

            // Normalize email for comparison
            email = email.ToLowerInvariant();

            // Attempt to find existing user
            var user = await _context.Accounts.FirstOrDefaultAsync(a => a.Email.ToLower() == email);

            if (user == null)
            {
                // Create a new user from the Google claims
                user = await CreateUserFromGoogleClaims(claims, email);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Failed to create user account.";
                    await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                    return RedirectToAction("Login");
                }
            }

            if (user.UserStatus != UserStatus.Active)
            {
                TempData["ErrorMessage"] = "Your account is not active.";
                await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }

            // Sign in using your cookie auth
            await SignInUser(user, isPersistent: true);

       


            // Redirect based on role using your helper method
            return RedirectBasedOnRole(user.Role);
        }




        private async Task<Account> CreateUserFromGoogleClaims(IEnumerable<Claim> claims, string email)
        {
            // Grab best available name pieces
            var fullName = GetFirstClaimValue(claims, ClaimTypes.Name, "name") ?? "";
            var givenName = GetFirstClaimValue(claims, ClaimTypes.GivenName, "given_name");
            var familyName = GetFirstClaimValue(claims, ClaimTypes.Surname, "family_name", "family-name");

            string name = "User";
            string surname = "";

            if (!string.IsNullOrEmpty(givenName) || !string.IsNullOrEmpty(familyName))
            {
                name = string.IsNullOrEmpty(givenName) ? (fullName.Split(' ').FirstOrDefault() ?? "User") : givenName;
                surname = familyName ?? (fullName.Contains(" ") ? fullName.Split(' ').Skip(1).FirstOrDefault() ?? "" : "");
            }
            else if (!string.IsNullOrEmpty(fullName))
            {
                var parts = fullName.Split(' ');
                name = parts.FirstOrDefault() ?? "User";
                surname = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";
            }

            // Create a new account with safe defaults (no plaintext password needed for OAuth users)
            var newUser = new Account
            {
                // Do NOT supply UserID; let the DB generate it (Identity)
                Name = name,
                Surname = surname,
                Title = "",
                Email = email,
                // Provide a generated password value (not used for OAuth login) to avoid null problems elsewhere
                Password = Guid.NewGuid().ToString("N"),
                Role = UserRole.Student,          // default role for new SSO users - change as desired
                UserStatus = UserStatus.Active,
                IsTwoFactorEnabled = false,
                TwoFactorSecretKey = null,
                TwoFactorRecoveryCodes = null
            };

            try
            {
                _context.Accounts.Add(newUser);
                await _context.SaveChangesAsync();
                return newUser;
            }
            catch
            {
                return null; // caller will show an appropriate error
            }
        }




        private string GenerateRandomPassword(int length = 16)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            var random = new Random();
            var chars = new char[length];

            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }

            return new string(chars);
        }



        [Authorize]
        [HttpPost]
        public async Task<IActionResult> LinkGoogleAccount()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null) return RedirectToAction("Login");

            var user = await _context.Accounts.FindAsync(int.Parse(userIdClaim.Value));
            if (user == null) return RedirectToAction("Login");

            // Store user ID in session for the callback
            HttpContext.Session.SetInt32("LinkAccountUserId", user.UserID);

            var redirectUrl = Url.Action("LinkGoogleResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> LinkGoogleResponse()
        {
            var userId = HttpContext.Session.GetInt32("LinkAccountUserId");
            if (userId == null) return RedirectToAction("Login");

            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Google authentication failed.";
                return RedirectToAction("ViewProfile");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var externalId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            var user = await _context.Accounts.FindAsync(userId.Value);
            if (user != null && user.Email == email)
            {
                user.ExternalProvider = "Google";
                user.ExternalProviderId = externalId;
                user.LastExternalLogin = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Google account linked successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Email doesn't match your account email.";
            }

            await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
            return RedirectToAction("ViewProfile");
        }



        private string GetFirstClaimValue(IEnumerable<Claim> claims, params string[] claimTypes)
        {
            foreach (var t in claimTypes)
            {
                var c = claims.FirstOrDefault(x => string.Equals(x.Type, t, StringComparison.OrdinalIgnoreCase));
                if (c != null && !string.IsNullOrEmpty(c.Value))
                    return c.Value;
            }
            return null;
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






        private const string TwoFactorRememberCookie = "SP.2FA.Remember";
        private static readonly TimeSpan RememberDuration = TimeSpan.FromDays(30);

        // Checks if the current browser has a valid "remembered device" token for this user
        private async Task<bool> IsRememberedDeviceValidAsync(int userId)
        {
            if (!Request.Cookies.TryGetValue(TwoFactorRememberCookie, out var rawToken) || string.IsNullOrWhiteSpace(rawToken))
                return false;

            var tokenHash = HashToken(rawToken);

            var record = await _context.RememberedDevices
                .FirstOrDefaultAsync(r => r.UserId == userId && r.TokenHash == tokenHash && !r.Revoked);

            if (record == null)
                return false;

            if (record.ExpiresAt <= DateTime.UtcNow)
            {
                // Clean up expired record + cookie
                _context.RememberedDevices.Remove(record);
                await _context.SaveChangesAsync();
                Response.Cookies.Delete(TwoFactorRememberCookie);
                return false;
            }

            return true;
        }

        // Stores a new "remember this device" token for the current browser
        private async Task RememberCurrentDeviceAsync(Account user, string? deviceName = null)
        {
            var rawToken = Guid.NewGuid().ToString("N"); // what goes to the cookie
            var tokenHash = HashToken(rawToken);         // what we store in DB

            var record = new RememberedDevice
            {
                UserId = user.UserID,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.Add(RememberDuration),
                DeviceName = deviceName,
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            _context.RememberedDevices.Add(record);
            await _context.SaveChangesAsync();

            // Persist a secure cookie with the raw token (only useful if paired with the hashed DB record)
            Response.Cookies.Append(
                TwoFactorRememberCookie,
                rawToken,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.Add(RememberDuration),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });
        }

        // Optional: revoke all remembered devices for a user (good to call on password change / disable 2FA)
        private async Task RevokeAllRememberedDevicesAsync(int userId)
        {
            var items = _context.RememberedDevices.Where(r => r.UserId == userId);
            _context.RememberedDevices.RemoveRange(items);
            await _context.SaveChangesAsync();

            // Delete cookie on this browser (other browsers still hold their own cookies)
            Response.Cookies.Delete(TwoFactorRememberCookie);
        }








    }
}