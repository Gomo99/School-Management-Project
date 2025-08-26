using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.ViewModel;
using X.PagedList.Extensions;
using SchoolProject.Service;

namespace SchoolProject.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        // Single constructor with all required dependencies
        public AdminController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        public async Task<IActionResult> DashboardAsync()
        {
            var currentUserName = User.Identity.Name;

            var model = new DashboardViewModel
            {
                ActiveModulesCount = await _context.Modules
                        .CountAsync(m => m.ModuleStatus == ModuleStatus.Active),
                InactiveModulesCount = await _context.Modules
                        .CountAsync(m => m.ModuleStatus == ModuleStatus.Inactive)
            };

            return View(model);
        }

        // View all modules
        public IActionResult ManageModules(int? page, string searchString)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var query = _context.Modules
                .Where(m => m.ModuleStatus == ModuleStatus.Active);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m =>
                    m.ModuleName.Contains(searchString) ||
                    m.ModuleType.ToString().Contains(searchString) ||
                    m.Duration.ToString().Contains(searchString));
            }

            var modules = query.OrderBy(m => m.ModuleName);
            return View(modules.ToPagedList(pageNumber, pageSize));
        }

        public IActionResult ExportModulesPdf(string searchString)
        {
            var query = _context.Modules
                .Where(m => m.ModuleStatus == ModuleStatus.Active);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m =>
                    m.ModuleName.Contains(searchString) ||
                    m.ModuleType.ToString().Contains(searchString) ||
                    m.Duration.ToString().Contains(searchString));
            }

            var modules = query.OrderBy(m => m.ModuleName).ToList();

            using var memoryStream = new MemoryStream();

            Document document = new Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter.GetInstance(document, memoryStream);
            document.Open();

            // Title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var title = new Paragraph("Module List", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 10f
            };
            document.Add(title);

            // Export info
            var infoFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.DarkGray);
            var exportInfo = new Paragraph($"Exported on: {DateTime.Now:dddd, dd MMMM yyyy HH:mm:ss}", infoFont)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingAfter = 20f
            };
            document.Add(exportInfo);

            // Table
            PdfPTable table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 3, 1, 2, 2 });

            AddCellToHeader(table, "Module Name");
            AddCellToHeader(table, "Duration");
            AddCellToHeader(table, "Type");
            AddCellToHeader(table, "Status");

            foreach (var module in modules)
            {
                AddCellToBody(table, module.ModuleName);
                AddCellToBody(table, module.Duration.ToString());
                AddCellToBody(table, module.ModuleType.ToString());
                AddCellToBody(table, module.ModuleStatus.ToString());
            }

            document.Add(table);
            document.Close();

            return File(memoryStream.ToArray(), "application/pdf", "Modules.pdf");
        }

        // GET: Create module
        public IActionResult CreateModule()
        {
            return View();
        }

        // POST: Create module
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModule(Module module)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    module.ModuleStatus = ModuleStatus.Active;
                    _context.Modules.Add(module);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Module created successfully!";
                    return RedirectToAction(nameof(ManageModules));
                }
                catch
                {
                    TempData["ErrorMessage"] = "An error occurred while creating the module.";
                    return RedirectToAction(nameof(ManageModules));
                }
            }

            TempData["ErrorMessage"] = "Invalid data. Please check the form.";
            return View(module);
        }

        // GET: Edit
        public async Task<IActionResult> EditModule(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null) return NotFound();
            return View(module);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModule(int id, Module module)
        {
            if (id != module.ModuleID)
            {
                TempData["ErrorMessage"] = "Module not found.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    module.ModuleStatus = ModuleStatus.Active;
                    _context.Update(module);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Module updated successfully!";
                    return RedirectToAction(nameof(ManageModules));
                }
                catch
                {
                    TempData["ErrorMessage"] = "An error occurred while updating the module.";
                    return RedirectToAction(nameof(ManageModules));
                }
            }

            TempData["ErrorMessage"] = "Invalid data. Please check the form.";
            return View(module);
        }

        // GET: Account/DeleteModule/5
        [HttpGet]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null) return NotFound();

            return View(module); // This should point to a view showing confirmation
        }

        // POST: Account/DeleteModule/5
        [HttpPost, ActionName("DeleteModule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModuleConfirmed(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null)
            {
                TempData["ErrorMessage"] = "Module not found.";
                return NotFound();
            }

            try
            {
                module.ModuleStatus = ModuleStatus.Inactive;
                _context.Modules.Update(module);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Module deleted successfully!";
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the module.";
            }

            return RedirectToAction(nameof(ManageModules));
        }



        // GET: Details
        public async Task<IActionResult> ModuleDetails(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null) return NotFound();

            return View(module);
        }














        public async Task<IActionResult> ManageLecturerModules()
        {
            var data = _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .Where(lm => lm.ModLecturerStatus == ModLecturerStatus.Active);

            return View(await data.ToListAsync());
        }


        public async Task<IActionResult> AddLecturerModule()
        {
            await PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLecturerModule(AssignLecturerModulesViewModel model)
        {
            if (model.SelectedModuleIDs == null || model.SelectedModuleIDs.Count == 0)
            {
                TempData["ErrorMessage"] = "Please select at least one module.";
                await PopulateDropdowns();
                return View(model);
            }

            // Get current count of active modules assigned to this lecturer
            var existingCount = await _context.LecturerModules
                .CountAsync(lm => lm.UserID == model.UserID && lm.ModLecturerStatus == ModLecturerStatus.Active);

            // Make sure the new total will not exceed 3
            if (existingCount + model.SelectedModuleIDs.Count > 3)
            {
                TempData["ErrorMessage"] = $"This lecturer is already assigned to {existingCount} module(s). " +
                                           $"You can only assign {3 - existingCount} more.";
                await PopulateDropdowns();
                return View(model);
            }

            try
            {
                foreach (var moduleId in model.SelectedModuleIDs)
                {
                    var lecturerModule = new LecturerModule
                    {
                        ModuleID = moduleId,
                        UserID = model.UserID,
                        AssignedDate = model.AssignedDate,
                        ModLecturerStatus = ModLecturerStatus.Active
                    };
                    _context.LecturerModules.Add(lecturerModule);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Lecturer successfully assigned to selected modules!";
                return RedirectToAction("ManageLecturerModules");
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while assigning modules.";
            }

            await PopulateDropdowns();
            return View(model);
        }





        
        // GET: EditLecturerModule
        public async Task<IActionResult> EditLecturerModule(int id)
        {
            var lecturerModule = await _context.LecturerModules.FindAsync(id);
            if (lecturerModule == null)
            {
                return NotFound();
            }

            await PopulateEditDropdowns(lecturerModule.ModuleID, lecturerModule.UserID);
            return View(lecturerModule);
        }


        // POST: EditLecturerModule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturerModule(LecturerModule lecturerModule)
        {
           
                try
                {
                    lecturerModule.ModLecturerStatus = ModLecturerStatus.Active;
                    _context.Update(lecturerModule);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Lecturer-module assignment updated successfully!";
                    return RedirectToAction("ManageLecturerModules");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.LecturerModules.Any(e => e.LecturerModuleID == lecturerModule.LecturerModuleID))
                    {
                        return NotFound();
                    }
                    TempData["ErrorMessage"] = "Concurrency error occurred. Please try again.";
                    throw;
                }
                catch
                {
                    TempData["ErrorMessage"] = "An error occurred while updating the lecturer-module assignment.";
                }
            

            await PopulateEditDropdowns(lecturerModule.ModuleID, lecturerModule.UserID);
            return View(lecturerModule);
        }




        // GET: DeleteLecturerModule
        // GET: DeleteLecturerModule
        public async Task<IActionResult> DeleteLecturerModule(int id)
        {
            var lecturerModule = await _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .FirstOrDefaultAsync(m => m.LecturerModuleID == id);

            if (lecturerModule == null)
                return NotFound();

            return View(lecturerModule);
        }


        // POST: DeleteLecturerModuleConfirmed
        [HttpPost, ActionName("DeleteLecturerModule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLecturerModuleConfirmed(int id)
        {
            var lecturerModule = await _context.LecturerModules.FindAsync(id);
            if (lecturerModule != null)
            {
                try
                {
                    lecturerModule.ModLecturerStatus = ModLecturerStatus.Inactive;
                    _context.LecturerModules.Update(lecturerModule);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Lecturer-module assignment deleted successfully!";
                }
                catch
                {
                    TempData["ErrorMessage"] = "An error occurred while deleting the lecturer-module assignment.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Lecturer-module assignment not found.";
            }

            return RedirectToAction("ManageLecturerModules");
        }



        // GET: DetailsLecturerModule
        public async Task<IActionResult> DetailsLecturerModule(int id)
        {
            var lecturerModule = await _context.LecturerModules
                .Include(lm => lm.Lecturer)  // Include Lecturer info
                .Include(lm => lm.Module)    // Include Module info
                .FirstOrDefaultAsync(m => m.LecturerModuleID == id);  // Get LecturerModule by ID

            if (lecturerModule == null)
            {
                return NotFound();  // Return Not Found if no record is found
            }

            return View(lecturerModule);  // Pass the lecturerModule to the view
        }












        public async Task<IActionResult> ManageStudentModule()
        {
            var studentModules = await _context.StudentModules
                .Where(sm => sm.StudModStatus == StudModStatus.Active) // Filter only active
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Lecturer)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Include(sm => sm.Student)
                .ToListAsync();

            return View(studentModules);
        }





        public IActionResult AddStudentModules()
        {
            // Populate lecturer dropdown
            var lecturerModules = _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .Select(lm => new {
                    lm.LecturerModuleID,
                    LecturerName = lm.Lecturer.Name + " " + lm.Lecturer.Surname + " - " + lm.Module.ModuleName
                })
                .ToList();

            ViewData["LecturerModuleID"] = new SelectList(lecturerModules, "LecturerModuleID", "LecturerName");

            // Populate active student dropdown
            var activeStudents = _context.Accounts
                .Where(a => a.Role == UserRole.Student && a.UserStatus == UserStatus.Active)
                .Select(a => new { a.UserID, FullName = a.Name + " " + a.Surname })
                .ToList();

            ViewData["UserID"] = new SelectList(activeStudents, "UserID", "FullName");

            return View();
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudentModules([Bind("StudentModuleID,LecturerModuleID,UserID,Date,StudModStatus")] StudentModule studentModule)
        {
            try
            {
                studentModule.StudModStatus = StudModStatus.Active;
                _context.Add(studentModule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Student-module enrollment added successfully!";
                return RedirectToAction(nameof(ManageStudentModule));
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while adding the student-module enrollment.";
            }

            // If model validation fails or an error occurs, repopulate the dropdowns
            ViewData["LecturerModuleID"] = new SelectList(_context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .Select(lm => new {
                    lm.LecturerModuleID,
                    LecturerName = lm.Lecturer.Name + " " + lm.Lecturer.Surname + " - " + lm.Module.ModuleName
                }),
                "LecturerModuleID", "LecturerName", studentModule.LecturerModuleID);

            ViewData["ModuleID"] = new SelectList(Enumerable.Empty<SelectListItem>());

            return View(studentModule);
        }



        public async Task<IActionResult> EditStudentModule(int id)
        {
            // Fetch the StudentModule to edit
            var studentModule = await _context.StudentModules
                .Include(sm => sm.LecturerModule)
                .ThenInclude(lm => lm.Lecturer)
                .Include(sm => sm.LecturerModule)
                .ThenInclude(lm => lm.Module)
                .Include(sm => sm.Student)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == id);

            if (studentModule == null)
            {
                return NotFound();
            }

            // Populate lecturer dropdown
            var lecturerModules = _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .Select(lm => new {
                    lm.LecturerModuleID,
                    LecturerName = lm.Lecturer.Name + " " + lm.Lecturer.Surname + " - " + lm.Module.ModuleName
                })
                .ToList();

            ViewData["LecturerModuleID"] = new SelectList(lecturerModules, "LecturerModuleID", "LecturerName", studentModule.LecturerModuleID);

            // Populate active student dropdown
            var activeStudents = _context.Accounts
                .Where(a => a.Role == UserRole.Student && a.UserStatus == UserStatus.Active)
                .Select(a => new { a.UserID, FullName = a.Name + " " + a.Surname })
                .ToList();

            ViewData["UserID"] = new SelectList(activeStudents, "UserID", "FullName", studentModule.UserID);

            return View(studentModule);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudentModule(int id, [Bind("StudentModuleID,LecturerModuleID,UserID,Date,StudModStatus")] StudentModule studentModule)
        {
            if (id != studentModule.StudentModuleID)
            {
                return NotFound();
            }

            
                try
                {
                    studentModule.StudModStatus = StudModStatus.Active;
                    _context.Update(studentModule);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Student-module enrollment updated successfully!";
                    return RedirectToAction(nameof(ManageStudentModule));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.StudentModules.Any(sm => sm.StudentModuleID == studentModule.StudentModuleID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "A concurrency error occurred while updating the enrollment.";
                        throw;
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = "An unexpected error occurred while updating the enrollment.";
                }
            

            // Repopulate dropdowns if model is invalid or an error occurs
            var lecturerModules = _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .Select(lm => new {
                    lm.LecturerModuleID,
                    LecturerName = lm.Lecturer.Name + " " + lm.Lecturer.Surname + " - " + lm.Module.ModuleName
                })
                .ToList();

            ViewData["LecturerModuleID"] = new SelectList(lecturerModules, "LecturerModuleID", "LecturerName", studentModule.LecturerModuleID);

            var activeStudents = _context.Accounts
                .Where(a => a.Role == UserRole.Student && a.UserStatus == UserStatus.Active)
                .Select(a => new { a.UserID, FullName = a.Name + " " + a.Surname })
                .ToList();

            ViewData["UserID"] = new SelectList(activeStudents, "UserID", "FullName", studentModule.UserID);

            return View(studentModule);
        }



        public async Task<IActionResult> DeleteStudentModule(int id)
        {
            var studentModule = await _context.StudentModules
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Lecturer)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == id);

            if (studentModule == null)
            {
                return NotFound();
            }

            return View(studentModule);
        }


        [HttpPost, ActionName("DeleteStudentModule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentModuleConfirmed(int id)
        {
            var studentModule = await _context.StudentModules.FindAsync(id);
            if (studentModule == null)
            {
                TempData["ErrorMessage"] = "Student-module enrollment not found.";
                return NotFound();
            }

            try
            {
                studentModule.StudModStatus = StudModStatus.Inactive;
                _context.StudentModules.Update(studentModule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Student-module enrollment deactivated successfully!";
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while deactivating the enrollment.";
            }

            return RedirectToAction(nameof(ManageStudentModule));
        }









        public async Task<IActionResult> ManageUsers()
        {
            // Get all active users ordered by surname and name
            var users = await _context.Accounts
                .OrderBy(u => u.Surname)
                .ThenBy(u => u.Name)
                .ToListAsync();

            return View(users);
        }







        // GET: Create User
        public IActionResult CreateUser()
        {
            PopulateRoleDropdown(); // No need to await, it's synchronous
            return View();
        }

        // POST: Create User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(Account account)
        {
            try
            {
                if (_context.Accounts.Any(a => a.Email == account.Email))
                {
                    TempData["ErrorMessage"] = "Email already exists in the system.";
                    PopulateRoleDropdown();
                    return View(account);
                }

                // Set status inactive initially
                account.UserStatus = UserStatus.Inactive;

                // Generate verification token
                account.EmailVerificationTokenHash = Guid.NewGuid().ToString();
                account.EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24);

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                // Send verification email
                var emailService = new EmailService(_configuration);
                var verificationLink = Url.Action(
                    "VerifyEmail",
                    "Account",
                    new { userId = account.UserID, token = account.EmailVerificationTokenHash },
                    Request.Scheme);

                var placeholders = new Dictionary<string, string>
        {
            { "Name", account.Name },
            { "VerificationLink", verificationLink }
        };

                await emailService.SendEmailWithTemplateAsync(
                    account.Email,
                    "Verify your email",
                    "EmailVerificationTemplate.html",
                    placeholders);

                TempData["SuccessMessage"] = $"User created! A verification email has been sent to {account.Email}.";
                return RedirectToAction(nameof(ManageUsers));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            PopulateRoleDropdown();
            return View(account);
        }




        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Accounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await PopulateRoleDropdown(user.Role);
            await PopulateStatusDropdown(user.UserStatus);
            return View(user);
        }

        // POST: Edit User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, Account account)
        {
            if (id != account.UserID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Prevent changing email to one that already exists (for another user)
                    if (_context.Accounts.Any(a => a.Email == account.Email && a.UserID != account.UserID))
                    {
                        TempData["ErrorMessage"] = "Email already exists in the system.";
                        await PopulateRoleDropdown(account.Role);
                        await PopulateStatusDropdown(account.UserStatus);
                        return View(account);
                    }

                    // Get the existing user from database
                    var existingUser = await _context.Accounts.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Update only the fields we want to allow changing
                    existingUser.Name = account.Name;
                    existingUser.Surname = account.Surname;
                    existingUser.Title = account.Title;
                    existingUser.Role = account.Role;
                    existingUser.Email = account.Email;

                    // Force status to Active regardless of input
                    existingUser.UserStatus = UserStatus.Active;

                    // Explicitly tell EF which properties to update
                    _context.Entry(existingUser).Property(x => x.Name).IsModified = true;
                    _context.Entry(existingUser).Property(x => x.Surname).IsModified = true;
                    _context.Entry(existingUser).Property(x => x.Title).IsModified = true;
                    _context.Entry(existingUser).Property(x => x.Role).IsModified = true;
                    _context.Entry(existingUser).Property(x => x.Email).IsModified = true;
                    _context.Entry(existingUser).Property(x => x.UserStatus).IsModified = true;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction(nameof(ManageUsers));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Accounts.Any(e => e.UserID == account.UserID))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await PopulateRoleDropdown(account.Role);
            await PopulateStatusDropdown(account.UserStatus);
            return View(account);
        }
        // GET: Delete User
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Accounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Delete User
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            var user = await _context.Accounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Soft delete by setting status to Inactive
                user.UserStatus = UserStatus.Inactive;
                _context.Accounts.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "User deactivated successfully!";
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while deactivating the user.";
            }

            return RedirectToAction(nameof(ManageUsers));
        }












        // Helper methods
        private async Task PopulateRoleDropdown(UserRole? selectedRole = null)
        {
            var roles = Enum.GetValues(typeof(UserRole))
                .Cast<UserRole>()
                .Select(r => new SelectListItem
                {
                    Value = r.ToString(),
                    Text = r.ToString(),
                    Selected = selectedRole.HasValue && r == selectedRole.Value
                })
                .ToList();

            ViewBag.Roles = new SelectList(roles, "Value", "Text", selectedRole);
        }

        private async Task PopulateStatusDropdown(UserStatus? selectedStatus = null)
        {
            var statuses = Enum.GetValues(typeof(UserStatus))
                .Cast<UserStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = selectedStatus.HasValue && s == selectedStatus.Value
                })
                .ToList();

            ViewBag.Statuses = new SelectList(statuses, "Value", "Text", selectedStatus);
        }




















































        private void AddCellToHeader(PdfPTable table, string text)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.White);
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = new BaseColor(52, 73, 94), // dark blue
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5
            };
            table.AddCell(cell);
        }

        private void AddCellToBody(PdfPTable table, string text)
        {
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.Black);
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5
            };
            table.AddCell(cell);
        }





        private async Task PopulateEditDropdowns(int currentModuleId, int currentLecturerId)
        {
            var assignedModuleIds = await _context.LecturerModules
                .Where(lm => lm.ModLecturerStatus == ModLecturerStatus.Active && lm.ModuleID != currentModuleId)
                .Select(lm => lm.ModuleID)
                .ToListAsync();

            var assignedLecturerIds = await _context.LecturerModules
                .Where(lm => lm.ModLecturerStatus == ModLecturerStatus.Active && lm.UserID != currentLecturerId)
                .Select(lm => lm.UserID)
                .ToListAsync();

            var availableModules = await _context.Modules
                .Where(m => m.ModuleStatus == ModuleStatus.Active && (!assignedModuleIds.Contains(m.ModuleID) || m.ModuleID == currentModuleId))
                .ToListAsync();

            var availableLecturers = await _context.Accounts
                .Where(u => u.Role == UserRole.Lecturer &&
                            u.UserStatus == UserStatus.Active &&
                            (!assignedLecturerIds.Contains(u.UserID) || u.UserID == currentLecturerId))
                .Select(u => new
                {
                    u.UserID,
                    FullName = u.Name + " " + u.Surname
                })
                .ToListAsync();
            ViewBag.ModuleList = new SelectList(availableModules, "ModuleID", "ModuleName", currentModuleId);
            ViewBag.LecturerList = new SelectList(availableLecturers, "UserID", "FullName", currentLecturerId);

        }





        private async Task PopulateDropdowns()
        {
            var availableModules = await _context.Modules
                .Where(m => m.ModuleStatus == ModuleStatus.Active)
                .ToListAsync();

            // Get lecturers with less than 3 assigned active modules
            var lecturersWithCounts = await _context.LecturerModules
                .Where(lm => lm.ModLecturerStatus == ModLecturerStatus.Active)
                .GroupBy(lm => lm.UserID)
                .Select(group => new
                {
                    UserID = group.Key,
                    Count = group.Count()
                })
                .ToListAsync();

            var lecturersToExclude = lecturersWithCounts
                .Where(x => x.Count >= 3)
                .Select(x => x.UserID)
                .ToList();

            var availableLecturers = await _context.Accounts
                .Where(u => u.Role == UserRole.Lecturer &&
                            u.UserStatus == UserStatus.Active &&
                            !lecturersToExclude.Contains(u.UserID))
                .Select(u => new
                {
                    u.UserID,
                    FullName = u.Name + " " + u.Surname
                })
                .ToListAsync();

            ViewBag.Modules = new MultiSelectList(availableModules, "ModuleID", "ModuleName");
            ViewBag.Lecturers = new SelectList(availableLecturers, "UserID", "FullName");
        }

        
       



    }
}
