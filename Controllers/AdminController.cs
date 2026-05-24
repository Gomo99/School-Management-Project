using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.Service;
using SchoolProject.Status;
using SchoolProject.ViewModel;
using System.Security.Claims;

namespace SchoolProject.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public AdminController(ApplicationDbContext context, NotificationService notificationService = null)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // ========== Dashboard ==========
        // Inside AdminController.cs
        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel
            {
                ActiveModulesCount = await _context.Modules.CountAsync(m => m.ModuleStatus == ModuleStatus.Active),
                InactiveModulesCount = await _context.Modules.CountAsync(m => m.ModuleStatus == ModuleStatus.Inactive),
                
            };
            return View(model);
        }

        // ========== USER MANAGEMENT ==========
        public async Task<IActionResult> ManageUsers(UserRole? role = null)
        {
            IQueryable<Account> users = _context.Accounts;
            if (role.HasValue)
                users = users.Where(u => u.Role == role.Value);
            var model = await users.ToListAsync();
            return View(model);
        }

        // GET: Admin/CreateUser
        public IActionResult CreateUser()
        {
            ViewBag.Roles = new SelectList(Enum.GetValues(typeof(UserRole)));
            return View();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(Account account)
        {
            if (ModelState.IsValid)
            {
                // Set defaults
                account.UserStatus = UserStatus.Active;
                account.Password = BCrypt.Net.BCrypt.HashPassword(account.Password); // hash password
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                // Send welcome notification to the new user
                if (_notificationService != null)
                {
                    string welcomeMsg = $"Welcome to SchoolProject! Your {account.Role} account has been created.";
                    await _notificationService.CreateAsync(account.UserID, welcomeMsg, "new_user");
                }

                TempData["SuccessMessage"] = "User created successfully.";
                return RedirectToAction(nameof(ManageUsers));
            }
            ViewBag.Roles = new SelectList(Enum.GetValues(typeof(UserRole)));
            return View(account);
        }

        // GET: Admin/EditUser/5
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Accounts.FindAsync(id);
            if (user == null) return NotFound();
            ViewBag.Roles = new SelectList(Enum.GetValues(typeof(UserRole)), user.Role);
            return View(user);
        }

        // POST: Admin/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, Account model)
        {
            if (id != model.UserID) return NotFound();
            if (ModelState.IsValid)
            {
                var user = await _context.Accounts.FindAsync(id);
                if (user == null) return NotFound();

                user.Name = model.Name;
                user.Surname = model.Surname;
                user.Title = model.Title;
                user.Email = model.Email;
                user.Role = model.Role;
                // Don't update password here – separate action

                _context.Update(user);
                await _context.SaveChangesAsync();

                // Optionally notify the user that their profile was updated
                if (_notificationService != null)
                {
                    await _notificationService.CreateAsync(user.UserID,
                        "Your profile has been updated by an administrator.", "profile_update");
                }

                TempData["SuccessMessage"] = "User updated.";
                return RedirectToAction(nameof(ManageUsers));
            }
            ViewBag.Roles = new SelectList(Enum.GetValues(typeof(UserRole)), model.Role);
            return View(model);
        }

        // GET: Admin/DeleteUser/5 (soft delete)
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Accounts.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            var user = await _context.Accounts.FindAsync(id);
            if (user == null) return NotFound();
            user.UserStatus = UserStatus.Inactive;
            _context.Update(user);
            await _context.SaveChangesAsync();

            // Notify the user of deactivation
            if (_notificationService != null)
            {
                await _notificationService.CreateAsync(user.UserID,
                    "Your account has been deactivated. Please contact the administrator.", "account_deactivated");
            }

            TempData["SuccessMessage"] = "User deactivated.";
            return RedirectToAction(nameof(ManageUsers));
        }

        // ========== MODULE MANAGEMENT ==========
        public async Task<IActionResult> ManageModules()
        {
            var modules = await _context.Modules.ToListAsync();
            return View(modules);
        }

        // GET: Admin/CreateModule
        public IActionResult CreateModule()
        {
            return View();
        }

        // POST: Admin/CreateModule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModule(Module module)
        {
            if (ModelState.IsValid)
            {
                module.ModuleStatus = ModuleStatus.Active;
                _context.Modules.Add(module);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Module created.";
                return RedirectToAction(nameof(ManageModules));
            }
            return View(module);
        }

        // GET: Admin/EditModule/5
        public async Task<IActionResult> EditModule(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null) return NotFound();
            return View(module);
        }

        // POST: Admin/EditModule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModule(int id, Module model)
        {
            if (id != model.ModuleID) return NotFound();
            if (ModelState.IsValid)
            {
                var module = await _context.Modules.FindAsync(id);
                if (module == null) return NotFound();
                module.ModuleName = model.ModuleName;
                module.Duration = model.Duration;
                module.ModuleType = model.ModuleType;
                _context.Update(module);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Module updated.";
                return RedirectToAction(nameof(ManageModules));
            }
            return View(model);
        }

        // GET: Admin/DeleteModule/5 (soft delete)
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null) return NotFound();
            return View(module);
        }

        [HttpPost, ActionName("DeleteModule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModuleConfirmed(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null) return NotFound();
            module.ModuleStatus = ModuleStatus.Inactive;
            _context.Update(module);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Module deactivated.";
            return RedirectToAction(nameof(ManageModules));
        }

        // ========== LECTURER MODULE ASSIGNMENT ==========
        public async Task<IActionResult> ManageLecturerModules()
        {
            var lecturerModules = await _context.LecturerModules
                .Include(lm => lm.Module)
                .Include(lm => lm.Lecturer)
                .ToListAsync();
            return View(lecturerModules);
        }

        // GET: Admin/AssignLecturer
        public IActionResult AssignLecturer()
        {
            // Fill lecturers dropdown
            ViewBag.Lecturers = new SelectList(
                _context.Accounts.Where(u => u.Role == UserRole.Lecturer && u.UserStatus == UserStatus.Active),
                "UserID", "Name");

            // Pass all active modules as a list for the checkboxes
            ViewBag.Modules = _context.Modules
                .Where(m => m.ModuleStatus == ModuleStatus.Active)
                .ToList();

            return View(new AssignLecturerModulesViewModel
            {
                AssignedDate = DateTime.Now,
                SelectedModuleIDs = new List<int>()
            });
        }

        // POST: Admin/AssignLecturer (now accepts multiple module IDs)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLecturer(int userId, List<int> moduleIds, DateTime assignedDate)
        {
            if (userId == 0 || moduleIds == null || moduleIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Please select a lecturer and at least one module.";
                // Re-populate dropdowns
                ViewBag.Lecturers = new SelectList(
                    _context.Accounts.Where(u => u.Role == UserRole.Lecturer && u.UserStatus == UserStatus.Active),
                    "UserID", "Name");
                ViewBag.Modules = _context.Modules
                    .Where(m => m.ModuleStatus == ModuleStatus.Active)
                    .ToList();
                return View(new AssignLecturerModulesViewModel
                {
                    UserID = userId,
                    SelectedModuleIDs = moduleIds ?? new List<int>(),
                    AssignedDate = assignedDate
                });
            }

            // Loop over each selected module and assign if not already active
            foreach (var moduleId in moduleIds)
            {
                bool exists = await _context.LecturerModules.AnyAsync(lm =>
                    lm.UserID == userId && lm.ModuleID == moduleId && lm.ModLecturerStatus == ModLecturerStatus.Active);
                if (!exists)
                {
                    var lecturerModule = new LecturerModule
                    {
                        UserID = userId,
                        ModuleID = moduleId,
                        AssignedDate = assignedDate,
                        ModLecturerStatus = ModLecturerStatus.Active
                    };
                    _context.LecturerModules.Add(lecturerModule);
                }
            }
            await _context.SaveChangesAsync();

            // Notify the lecturer
            if (_notificationService != null)
            {
                var moduleNames = await _context.Modules
                    .Where(m => moduleIds.Contains(m.ModuleID))
                    .Select(m => m.ModuleName)
                    .ToListAsync();
                string msg = $"You have been assigned to the following modules: {string.Join(", ", moduleNames)}.";
                await _notificationService.CreateAsync(userId, msg, "lecturer_assigned");
            }

            TempData["SuccessMessage"] = "Lecturer assigned to selected modules.";
            return RedirectToAction(nameof(ManageLecturerModules));
        }


        // GET: Admin/ChangeLecturer/5 (LecturerModuleID)
        public async Task<IActionResult> ChangeLecturer(int id)
        {
            var lm = await _context.LecturerModules.Include(lm => lm.Lecturer).FirstOrDefaultAsync(lm => lm.LecturerModuleID == id);
            if (lm == null) return NotFound();
            ViewBag.Lecturers = new SelectList(_context.Accounts.Where(u => u.Role == UserRole.Lecturer && u.UserStatus == UserStatus.Active), "UserID", "Name", lm.UserID);
            return View(lm);
        }

        // POST: Admin/ChangeLecturer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeLecturer(int id, int newLecturerId)
        {
            var existing = await _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .FirstOrDefaultAsync(lm => lm.LecturerModuleID == id);

            if (existing == null) return NotFound();

            int oldLecturerId = existing.UserID;
            string moduleName = existing.Module?.ModuleName ?? "a module";

            // 1. Deactivate current assignment
            existing.ModLecturerStatus = ModLecturerStatus.Inactive;
            _context.Update(existing);

            // 2. Create new active assignment
            var newAssignment = new LecturerModule
            {
                ModuleID = existing.ModuleID,
                UserID = newLecturerId,
                AssignedDate = DateTime.Now,
                ModLecturerStatus = ModLecturerStatus.Active
            };
            _context.LecturerModules.Add(newAssignment);
            await _context.SaveChangesAsync();

            // Send notifications
            if (_notificationService != null)
            {
                // Notify old lecturer (if still active)
                var oldLecturer = await _context.Accounts.FindAsync(oldLecturerId);
                if (oldLecturer != null && oldLecturer.UserStatus == UserStatus.Active)
                {
                    string msgOld = $"You have been removed from the module \"{moduleName}\".";
                    await _notificationService.CreateAsync(oldLecturerId, msgOld, "lecturer_removed");
                }

                // Notify new lecturer
                string msgNew = $"You have been assigned to the module \"{moduleName}\".";
                await _notificationService.CreateAsync(newLecturerId, msgNew, "lecturer_assigned");
            }

            TempData["SuccessMessage"] = "Lecturer changed successfully.";
            return RedirectToAction(nameof(ManageLecturerModules));
        }


        public async Task<IActionResult> EditLecturerModule(int id)
        {
            var lm = await _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .FirstOrDefaultAsync(lm => lm.LecturerModuleID == id);
            if (lm == null) return NotFound();

            ViewBag.LecturerList = new SelectList(_context.Accounts.Where(u => u.Role == UserRole.Lecturer && u.UserStatus == UserStatus.Active), "UserID", "Name", lm.UserID);
            ViewBag.ModuleList = new SelectList(_context.Modules.Where(m => m.ModuleStatus == ModuleStatus.Active), "ModuleID", "ModuleName", lm.ModuleID);
            return View(lm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturerModule(int id, LecturerModule model)
        {
            if (id != model.LecturerModuleID) return NotFound();
            if (ModelState.IsValid)
            {
                var existing = await _context.LecturerModules.FindAsync(id);
                if (existing == null) return NotFound();

                existing.UserID = model.UserID;
                existing.ModuleID = model.ModuleID;
                existing.AssignedDate = model.AssignedDate;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                if (_notificationService != null)
                {
                    var moduleName = (await _context.Modules.FindAsync(model.ModuleID))?.ModuleName;
                    await _notificationService.CreateAsync(model.UserID,
                        $"Your assignment for module \"{moduleName}\" has been updated by an administrator.", "lecturer_updated");
                }

                TempData["SuccessMessage"] = "Lecturer module assignment updated.";
                return RedirectToAction(nameof(ManageLecturerModules));
            }
            ViewBag.LecturerList = new SelectList(_context.Accounts.Where(u => u.Role == UserRole.Lecturer && u.UserStatus == UserStatus.Active), "UserID", "Name", model.UserID);
            ViewBag.ModuleList = new SelectList(_context.Modules.Where(m => m.ModuleStatus == ModuleStatus.Active), "ModuleID", "ModuleName", model.ModuleID);
            return View(model);
        }

        // ========== MISSING: Delete Lecturer Module (Soft) ==========
        public async Task<IActionResult> DeleteLecturerModule(int id)
        {
            var lm = await _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .FirstOrDefaultAsync(lm => lm.LecturerModuleID == id);
            if (lm == null) return NotFound();
            return View(lm);
        }

        [HttpPost, ActionName("DeleteLecturerModule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLecturerModuleConfirmed(int id)
        {
            var lm = await _context.LecturerModules
                .Include(lm => lm.Lecturer)
                .Include(lm => lm.Module)
                .FirstOrDefaultAsync(lm => lm.LecturerModuleID == id);
            if (lm == null) return NotFound();

            lm.ModLecturerStatus = ModLecturerStatus.Inactive;
            _context.Update(lm);
            await _context.SaveChangesAsync();

            if (_notificationService != null && lm.Lecturer != null)
            {
                await _notificationService.CreateAsync(lm.UserID,
                    $"Your assignment for module \"{lm.Module?.ModuleName}\" has been removed by an administrator.",
                    "lecturer_removed");
            }

            TempData["SuccessMessage"] = "Lecturer module assignment deactivated.";
            return RedirectToAction(nameof(ManageLecturerModules));
        }

        // ========== STUDENT ENROLLMENT ==========
        public async Task<IActionResult> ManageStudentModules()
        {
            var studentModules = await _context.StudentModules
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Lecturer)
                .ToListAsync();
            return View(studentModules);
        }

        // GET: Admin/EnrollStudent
        public IActionResult EnrollStudent()
        {
            ViewBag.Students = new SelectList(_context.Accounts.Where(u => u.Role == UserRole.Student && u.UserStatus == UserStatus.Active), "UserID", "Name");
            // Show active lecturer‑module pair with module name
            ViewBag.LecturerModules = new SelectList(
                _context.LecturerModules
                    .Where(lm => lm.ModLecturerStatus == ModLecturerStatus.Active)
                    .Include(lm => lm.Module)
                    .Select(lm => new { lm.LecturerModuleID, Display = lm.Module.ModuleName + " - " + lm.Lecturer.Name }),
                "LecturerModuleID", "Display");
            return View();
        }

        // POST: Admin/EnrollStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudent(int userId, int lecturerModuleId)
        {
            bool alreadyEnrolled = await _context.StudentModules.AnyAsync(sm =>
                sm.UserID == userId && sm.LecturerModuleID == lecturerModuleId && sm.StudModStatus == StudModStatus.Active);
            if (alreadyEnrolled)
            {
                TempData["ErrorMessage"] = "Student already enrolled.";
                return RedirectToAction(nameof(EnrollStudent));
            }
            var studentModule = new StudentModule
            {
                UserID = userId,
                LecturerModuleID = lecturerModuleId,
                Date = DateTime.Now,
                StudModStatus = StudModStatus.Active
            };
            _context.StudentModules.Add(studentModule);
            await _context.SaveChangesAsync();

            // Notify the student
            if (_notificationService != null)
            {
                var lm = await _context.LecturerModules
                    .Include(x => x.Module)
                    .Include(x => x.Lecturer)
                    .FirstOrDefaultAsync(x => x.LecturerModuleID == lecturerModuleId);
                if (lm != null)
                {
                    string msg = $"You have been enrolled in \"{lm.Module.ModuleName}\" taught by {lm.Lecturer.Title} {lm.Lecturer.Name} {lm.Lecturer.Surname}.";
                    await _notificationService.CreateAsync(userId, msg, "student_enrolled");
                }
            }

            TempData["SuccessMessage"] = "Student enrolled.";
            return RedirectToAction(nameof(ManageStudentModules));
        }



        public async Task<IActionResult> EditStudentModule(int id)
        {
            var sm = await _context.StudentModules
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == id);
            if (sm == null) return NotFound();

            ViewData["UserID"] = new SelectList(_context.Accounts.Where(u => u.Role == UserRole.Student && u.UserStatus == UserStatus.Active), "UserID", "Name", sm.UserID);
            ViewData["LecturerModuleID"] = new SelectList(
                _context.LecturerModules
                    .Where(lm => lm.ModLecturerStatus == ModLecturerStatus.Active)
                    .Include(lm => lm.Module)
                    .Include(lm => lm.Lecturer)
                    .Select(lm => new { lm.LecturerModuleID, Display = lm.Module.ModuleName + " - " + lm.Lecturer.Name }),
                "LecturerModuleID", "Display", sm.LecturerModuleID);
            return View(sm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudentModule(int id, StudentModule model)
        {
            if (id != model.StudentModuleID) return NotFound();
            if (ModelState.IsValid)
            {
                var existing = await _context.StudentModules.FindAsync(id);
                if (existing == null) return NotFound();

                existing.UserID = model.UserID;
                existing.LecturerModuleID = model.LecturerModuleID;
                existing.Date = model.Date;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                if (_notificationService != null)
                {
                    var lm = await _context.LecturerModules
                        .Include(x => x.Module)
                        .Include(x => x.Lecturer)
                        .FirstOrDefaultAsync(x => x.LecturerModuleID == model.LecturerModuleID);
                    await _notificationService.CreateAsync(model.UserID,
                        $"Your enrollment details have been updated by an administrator.", "enrollment_updated");
                }

                TempData["SuccessMessage"] = "Student module enrollment updated.";
                return RedirectToAction(nameof(ManageStudentModules));
            }
            ViewData["UserID"] = new SelectList(_context.Accounts.Where(u => u.Role == UserRole.Student && u.UserStatus == UserStatus.Active), "UserID", "Name", model.UserID);
            ViewData["LecturerModuleID"] = new SelectList(
                _context.LecturerModules
                    .Where(lm => lm.ModLecturerStatus == ModLecturerStatus.Active)
                    .Include(lm => lm.Module)
                    .Include(lm => lm.Lecturer)
                    .Select(lm => new { lm.LecturerModuleID, Display = lm.Module.ModuleName + " - " + lm.Lecturer.Name }),
                "LecturerModuleID", "Display", model.LecturerModuleID);
            return View(model);
        }

        // ========== MISSING: Delete Student Module (Soft) ==========
        public async Task<IActionResult> DeleteStudentModule(int id)
        {
            var sm = await _context.StudentModules
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Lecturer)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == id);
            if (sm == null) return NotFound();
            return View(sm);
        }

        [HttpPost, ActionName("DeleteStudentModule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentModuleConfirmed(int id)
        {
            var sm = await _context.StudentModules
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == id);
            if (sm == null) return NotFound();

            sm.StudModStatus = StudModStatus.Inactive;
            _context.Update(sm);
            await _context.SaveChangesAsync();

            if (_notificationService != null && sm.Student != null)
            {
                await _notificationService.CreateAsync(sm.UserID,
                    $"Your enrollment in module \"{sm.LecturerModule?.Module?.ModuleName}\" has been removed by an administrator.",
                    "enrollment_removed");
            }

            TempData["SuccessMessage"] = "Student enrollment deactivated.";
            return RedirectToAction(nameof(ManageStudentModules));
        }





        // ========== MISSING: ASSESSMENT TYPES CRUD (Admin) ==========
        public async Task<IActionResult> ManageAssessmentTypes()
        {
            var types = await _context.AssessmentTypes.Where(t => !t.IsDeleted).ToListAsync();
            return View(types);
        }

        public IActionResult AddAssessmentType()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssessmentType(AssessmentType type)
        {
            if (ModelState.IsValid)
            {
                type.IsDeleted = false;
                _context.AssessmentTypes.Add(type);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Assessment type created.";
                return RedirectToAction(nameof(ManageAssessmentTypes));
            }
            return View(type);
        }

        public async Task<IActionResult> EditAssessmentType(int? id)
        {
            if (id == null) return NotFound();
            var type = await _context.AssessmentTypes.FindAsync(id);
            if (type == null || type.IsDeleted) return NotFound();
            return View(type);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessmentType(int id, AssessmentType model)
        {
            if (id != model.AssessmentTypeID) return NotFound();
            if (ModelState.IsValid)
            {
                var existing = await _context.AssessmentTypes.FindAsync(id);
                if (existing == null || existing.IsDeleted) return NotFound();
                existing.AssessmentTypeDescription = model.AssessmentTypeDescription;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Assessment type updated.";
                return RedirectToAction(nameof(ManageAssessmentTypes));
            }
            return View(model);
        }

        public async Task<IActionResult> DeleteAssessmentType(int? id)
        {
            if (id == null) return NotFound();
            var type = await _context.AssessmentTypes.FindAsync(id);
            if (type == null || type.IsDeleted) return NotFound();
            return View(type);
        }

        [HttpPost, ActionName("DeleteAssessmentType")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessmentTypeConfirmed(int id)
        {
            var type = await _context.AssessmentTypes.FindAsync(id);
            if (type != null && !type.IsDeleted)
            {
                type.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            TempData["SuccessMessage"] = "Assessment type deleted.";
            return RedirectToAction(nameof(ManageAssessmentTypes));
        }







        // ========== REPORTS ==========
        // 1. User Report
        public async Task<IActionResult> UserReport(string searchTerm, UserRole? role = null)
        {
            IQueryable<Account> query = _context.Accounts;
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Name.Contains(searchTerm) || u.Surname.Contains(searchTerm) || u.UserID.ToString().Contains(searchTerm));
            }
            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }
            var users = await query.ToListAsync();
            return View(users);
        }

        // 2. Module Report
        public async Task<IActionResult> ModuleReport(int? moduleId)
        {
            if (moduleId == null)
            {
                // List all modules with basic info
                var modules = await _context.Modules.ToListAsync();
                return View("ModuleReportList", modules);
            }
            else
            {
                var module = await _context.Modules.FindAsync(moduleId);
                if (module == null) return NotFound();
                var activeLecturer = await _context.LecturerModules
                    .Where(lm => lm.ModuleID == moduleId && lm.ModLecturerStatus == ModLecturerStatus.Active)
                    .Include(lm => lm.Lecturer)
                    .FirstOrDefaultAsync();
                int studentCount = await _context.StudentModules.CountAsync(sm =>
                    sm.LecturerModule.ModuleID == moduleId && sm.StudModStatus == StudModStatus.Active);
                ViewBag.Module = module;
                ViewBag.Lecturer = activeLecturer?.Lecturer;
                ViewBag.StudentCount = studentCount;
                return View("ModuleReportDetail");
            }
        }

        // 3. Assessment Report – filtered by status, type, date range
        public async Task<IActionResult> AssessmentReport(AssessmentStatus? status, int? typeId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Assessments
                .Where(a => !a.IsDeleted)
                .Include(a => a.AssessmentType)
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.Student)
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.LecturerModule)
                        .ThenInclude(lm => lm.Module)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(a => a.AssessmentStatus == status.Value);
            if (typeId.HasValue)
                query = query.Where(a => a.AssessmentTypeID == typeId.Value);
            if (fromDate.HasValue)
                query = query.Where(a => a.DueDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(a => a.DueDate <= toDate.Value);

            var assessments = await query.OrderBy(a => a.DueDate).ToListAsync();

            ViewBag.StatusOptions = Enum.GetValues(typeof(AssessmentStatus)).Cast<AssessmentStatus>().ToList();
            ViewBag.AssessmentTypes = await _context.AssessmentTypes.Where(t => !t.IsDeleted).ToListAsync();
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedTypeId = typeId;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(assessments);
        }



        // GET: Admin/ManageParentLinks
        public async Task<IActionResult> ManageParentLinks()
        {
            var links = await _context.StudentParentLinks
                .Include(l => l.Parent)
                .Include(l => l.Student)
                .ToListAsync();
            return View(links);
        }

        // GET: Admin/AddParentLink
        public IActionResult AddParentLink()
        {
            ViewBag.Parents = new SelectList(
                _context.Accounts.Where(u => u.Role == UserRole.Parent && u.UserStatus == UserStatus.Active),
                "UserID", "Name");
            ViewBag.Students = new SelectList(
                _context.Accounts.Where(u => u.Role == UserRole.Student && u.UserStatus == UserStatus.Active),
                "UserID", "Name");
            return View();
        }

        // POST: Admin/AddParentLink
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddParentLink(int parentUserId, int studentUserId)
        {
            if (parentUserId == 0 || studentUserId == 0)
            {
                TempData["ErrorMessage"] = "Both parent and student must be selected.";
                return RedirectToAction(nameof(AddParentLink));
            }

            bool exists = await _context.StudentParentLinks.AnyAsync(l =>
                l.ParentUserId == parentUserId && l.StudentUserId == studentUserId);
            if (!exists)
            {
                var link = new StudentParentLink
                {
                    ParentUserId = parentUserId,
                    StudentUserId = studentUserId
                };
                _context.StudentParentLinks.Add(link);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Link created.";
            }
            else
            {
                TempData["ErrorMessage"] = "This link already exists.";
            }
            return RedirectToAction(nameof(ManageParentLinks));
        }

        // POST: Admin/DeleteParentLink/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteParentLink(int id)
        {
            var link = await _context.StudentParentLinks.FindAsync(id);
            if (link != null)
            {
                _context.StudentParentLinks.Remove(link);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageParentLinks));
        }

    }
}