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

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Account account)
        {
            // Password strength validation
            var passwordRegex = new Regex(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$");
            if (!passwordRegex.IsMatch(account.Password))
            {
                TempData["ErrorMessage"] = "Password must be at least 6 characters long, contain at least one uppercase letter, one number, and one special character.";
                return View(account);
            }

            // Check if the email already exists in the database
            if (_context.Accounts.Any(a => a.Email == account.Email))
            {
                TempData["ErrorMessage"] = "This email is already in use.";
                return View(account); // Return to the view with the error message
            }

            // Ensure that the user status is set to Active during registration
            account.UserStatus = UserStatus.Active;

            // Hash the password using bcrypt before saving it to the database
            account.Password = BCrypt.Net.BCrypt.HashPassword(account.Password);

            // Save the account with the hashed password and the active user status
            _context.Accounts.Add(account);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Registration successful! You can now log in.";
            // Redirect to login after successful registration
            return RedirectToAction("Login", "Account");
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

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                TempData["ErrorMessage"] = "Invalid login attempt or account is inactive.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Login successful!";
            TempData["UserName"] = user.Name;

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Role, user.Role.ToString())
    };

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
                string resetPin = new Random().Next(100000, 999999).ToString();

                // Hash the reset PIN before saving it
                string hashedResetPin = BCrypt.Net.BCrypt.HashPassword(resetPin);

                // Prepare placeholders for dynamic content
                var placeholders = new Dictionary<string, string>
        {
            { "Name", user.Name },
            { "Email", email },
            { "ResetPin", resetPin }  // Send the plain PIN to the user in the email
        };

                // Send email using the template
                var subject = "Password Reset Request - SchoolProject";
                await _emailService.SendEmailWithTemplateAsync(email, subject, "password-reset-template.html", placeholders);




                // Save the hashed PIN and expiration time in the database
                user.ResetPin = hashedResetPin;
                user.ResetPinExpiration = DateTime.Now.AddMinutes(5);
                await _context.SaveChangesAsync();

                return View("ResetPassword");
            }

            ModelState.AddModelError(string.Empty, "The email address you entered is not associated with any account.");
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

            // Validate the PIN from the database (assuming you store it as a hashed value)
            var user = _context.Accounts.FirstOrDefault(a => a.ResetPinExpiration > DateTime.Now);

            if (user != null && BCrypt.Net.BCrypt.Verify(pin, user.ResetPin))  // Verify the hashed PIN
            {
                // If the PIN is correct, hash the new password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // Update the user's password with the new hashed password
                user.Password = hashedPassword;

                // Clear the ResetPin after successful password change (for security reasons)
                user.ResetPin = null;

                // Save changes to the database
                _context.SaveChanges();

                // Redirect to login page after successful password reset
                return RedirectToAction("Login");
            }

            // If PIN is invalid, return an error message
            ModelState.AddModelError(string.Empty, "Invalid PIN.");
            return View();
        }




        [Authorize]
        [HttpGet]
        public IActionResult EditProfile()
        {
            // Get the current user from the context
            var currentUserName = User.Identity.Name;
            var user = _context.Accounts.FirstOrDefault(a => a.Name == currentUserName);

            if (user == null)
            {
                return NotFound();  // If user is not found
            }

            // Create a ViewModel for profile editing (optional)
            var model = new EditProfileViewModel
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                Title = user.Title
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUserName = User.Identity.Name;
                var user = _context.Accounts.FirstOrDefault(a => a.Name == currentUserName);

                if (user == null)
                {
                    return NotFound();
                }

                // Check if the new email is taken by another user
                var existingUserWithEmail = _context.Accounts
                    .FirstOrDefault(a => a.Email == model.Email && a.UserID != user.UserID);

                if (existingUserWithEmail != null)
                {
                    ModelState.AddModelError("Email", "This email is already in use by another user.");
                    return View(model);
                }

                // Store old email before change
                var oldEmail = user.Email;

                // Update user info
                user.Name = model.Name;
                user.Surname = model.Surname;
                user.Email = model.Email;
                user.Title = model.Title;
                user.PhoneNumber = model.PhoneNumber;
                _context.SaveChanges();

                // Send security alerts if email was changed
                if (oldEmail != user.Email)
                {
                    var changeDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy hh:mm tt");

                    // 1. Alert to old email
                    var oldEmailPlaceholders = new Dictionary<string, string>
            {
                { "Name", user.Name },
                { "OldEmail", oldEmail },
                { "NewEmail", user.Email },
                { "ChangeDate", changeDate }
            };

                    await _emailService.SendEmailWithTemplateAsync(
                        oldEmail,
                        "Security Alert: Your Email Address Was Changed",
                        "EmailChangeTemplate.html",
                        oldEmailPlaceholders
                    );

                    // 2. Alert to new email (with full profile details)
                    var newEmailPlaceholders = new Dictionary<string, string>
            {
                { "Name", user.Name },
                { "Surname", user.Surname },
                { "Title", user.Title },
                { "OldEmail", oldEmail },
                { "NewEmail", user.Email },
                { "ChangeDate", changeDate },
                { "Role", user.Role.ToString() },
                { "Status", user.UserStatus.ToString() }
            };

                    await _emailService.SendEmailWithTemplateAsync(
                        user.Email,
                        "Welcome! Your Email Address Has Been Updated",
                        "EmailChangeAlert_New.html",
                        newEmailPlaceholders
                    );
                }

                TempData["SuccessMessage"] = "Your profile has been updated successfully.";

                return user.Role switch
                {
                    UserRole.Administrator => RedirectToAction("Dashboard", "Admin"),
                    UserRole.Lecturer => RedirectToAction("Dashboard", "Lecturer"),
                    UserRole.Student => RedirectToAction("Dashboard", "Student"),
                    _ => RedirectToAction("Index", "Home"),
                };
            }

            return View(model);
        }


        [Authorize]
        [HttpGet]
        public IActionResult ViewProfile()
        {
            var currentUserName = User.Identity.Name;
            var user = _context.Accounts.FirstOrDefault(a => a.Name == currentUserName);

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
                PhoneNumber = user.PhoneNumber,
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
            if (ModelState.IsValid)
            {
                var username = User.Identity.Name;
                var user = _context.Accounts.FirstOrDefault(a => a.Name == username);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return NotFound();
                }

                // Check if the current password is correct
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return View(model);
                }

                // Ensure that the new password is not the same as the current password
                if (model.CurrentPassword == model.NewPassword)
                {
                    TempData["ErrorMessage"] = "The new password cannot be the same as the current password.";
                    return View(model);
                }

                // Hash and update the new password
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _context.SaveChanges();

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

            TempData["ErrorMessage"] = "Password change failed.";
            return View(model);
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
        public IActionResult ReactivateAccount(string search)
        {
            var users = _context.Accounts
                .Where(u => u.UserStatus == UserStatus.Inactive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    u.Name.Contains(search) ||
                    u.Surname.Contains(search) ||
                    u.Email.Contains(search));
            }

            return View(users.ToList());
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
            var userName = User.Identity.Name;
            var user = await _context.Accounts.FirstOrDefaultAsync(a => a.Name == userName);

            if (user == null)
            {
                return NotFound();
            }

            var userProfile = _context.Accounts
                .Where(u => u.UserID == user.UserID)
                .Select(u => new ViewProfileViewModel
                {
                    Title = u.Title,
                    Name = u.Name,
                    Surname = u.Surname,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
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
            document.Add(new Paragraph($"Phone Number: {userProfile.PhoneNumber}", textFont));
            document.Add(new Paragraph($"Role: {userProfile.Role}", textFont));
            document.Add(new Paragraph($"Status: {userProfile.UserStatus}", textFont));

            document.Close();
            memoryStream.Position = 0;

            // Generate dynamic filename (e.g., "Profile_JohnDoe_20240414.pdf")
            var fileName = $"Profile_{user.Name}_{user.Surname}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(memoryStream, "application/pdf", fileName);
        }









    }
}

