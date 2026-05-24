using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.Service;
using SchoolProject.Status;
using System.Security.Claims;

namespace SchoolProject.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        public LecturerController(ApplicationDbContext context, NotificationService notificationService = null)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue("UserID"));
        }

        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            var lecturerModules = await _context.LecturerModules
                .Where(lm => lm.UserID == userId && lm.ModLecturerStatus == ModLecturerStatus.Active)
                .ToListAsync();
            var lecturerModuleIds = lecturerModules.Select(lm => lm.LecturerModuleID).ToList();

            ViewBag.ModuleCount = lecturerModules.Count;
            ViewBag.StudentCount = await _context.StudentModules
                .Where(sm => lecturerModuleIds.Contains(sm.LecturerModuleID) && sm.StudModStatus == StudModStatus.Active)
                .Select(sm => sm.UserID)
                .Distinct()
                .CountAsync();

            var assessments = await _context.Assessments
                .Where(a => !a.IsDeleted && lecturerModuleIds.Contains(a.StudentModule.LecturerModuleID))
                .ToListAsync();
            ViewBag.AssessmentCount = assessments.Count;
            ViewBag.CompletedCount = assessments.Count(a => a.AssessmentStatus == AssessmentStatus.Completed);

            return View();
        }

        // List my active modules
        public async Task<IActionResult> ListModules()
        {
            var userId = GetCurrentUserId();
            var lecturerModules = await _context.LecturerModules
                .Include(lm => lm.Module)
                .Where(lm => lm.UserID == userId && lm.ModLecturerStatus == ModLecturerStatus.Active)
                .ToListAsync();
            return View(lecturerModules);
        }

        // ===== ASSESSMENT TYPE CRUD (already correct, but we switch to IsDeleted) =====
        public async Task<IActionResult> ManageAssessmentType()
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
                _context.Add(type);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageAssessmentType));
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
        public async Task<IActionResult> EditAssessmentType(int id, AssessmentType type)
        {
            if (id != type.AssessmentTypeID) return NotFound();
            if (ModelState.IsValid)
            {
                var existing = await _context.AssessmentTypes.FindAsync(id);
                if (existing == null || existing.IsDeleted) return NotFound();
                existing.AssessmentTypeDescription = type.AssessmentTypeDescription;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageAssessmentType));
            }
            return View(type);
        }

        // Soft delete
        public async Task<IActionResult> DeleteAssessmentType(int? id)
        {
            if (id == null) return NotFound();
            var type = await _context.AssessmentTypes.FindAsync(id);
            if (type == null || type.IsDeleted) return NotFound();
            return View(type);
        }

        [HttpPost, ActionName("DeleteAssessmentType")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var type = await _context.AssessmentTypes.FindAsync(id);
            if (type != null && !type.IsDeleted)
            {
                type.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageAssessmentType));
        }

        // ===== ASSESSMENT MANAGEMENT – SCOPED TO OWN MODULES =====
        public async Task<IActionResult> ManageAssessments()
        {
            var userId = GetCurrentUserId();
            var lecturerModuleIds = await _context.LecturerModules
                .Where(lm => lm.UserID == userId && lm.ModLecturerStatus == ModLecturerStatus.Active)
                .Select(lm => lm.LecturerModuleID)
                .ToListAsync();

            var assessments = await _context.Assessments
                .Where(a => !a.IsDeleted && lecturerModuleIds.Contains(a.StudentModule.LecturerModuleID))
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.Student)
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.LecturerModule)
                        .ThenInclude(lm => lm.Module)
                .Include(a => a.AssessmentType)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            return View(assessments);
        }

        // GET: AddAssessment – only show student modules of my own modules
        public IActionResult AddAssessment()
        {
            var userId = GetCurrentUserId();
            var studentModules = _context.StudentModules
                .Where(sm => sm.LecturerModule.UserID == userId && sm.LecturerModule.ModLecturerStatus == ModLecturerStatus.Active && sm.StudModStatus == StudModStatus.Active)
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Select(sm => new {
                    sm.StudentModuleID,
                    DisplayName = sm.Student.Name + " " + sm.Student.Surname + " - " + sm.LecturerModule.Module.ModuleName
                })
                .ToList();

            ViewData["StudentModuleID"] = new SelectList(studentModules, "StudentModuleID", "DisplayName");
            ViewData["AssessmentTypeID"] = new SelectList(_context.AssessmentTypes.Where(t => !t.IsDeleted), "AssessmentTypeID", "AssessmentTypeDescription");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssessment([Bind("StudentModuleID,AssessmentTypeID,DueDate")] Assessment assessment)
        {
            // Verify that the StudentModule belongs to this lecturer
            var userId = GetCurrentUserId();
            var studentModule = await _context.StudentModules
                .Include(sm => sm.LecturerModule)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == assessment.StudentModuleID);
            if (studentModule == null || studentModule.LecturerModule.UserID != userId)
            {
                TempData["ErrorMessage"] = "Invalid student module.";
                return RedirectToAction(nameof(AddAssessment));
            }

            assessment.AssessmentStatus = AssessmentStatus.NotStarted;
            assessment.IsDeleted = false;
            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();

            // Notify the student
            if (_notificationService != null)
            {
                var student = await _context.StudentModules
                    .Include(sm => sm.Student)
                    .FirstOrDefaultAsync(sm => sm.StudentModuleID == assessment.StudentModuleID);

                if (student != null)
                {
                    string message = $"New assessment: {student.LecturerModule?.Module?.ModuleName} " +
                                    $"– {_context.AssessmentTypes.Find(assessment.AssessmentTypeID)?.AssessmentTypeDescription} " +
                                    $"due on {assessment.DueDate:dd MMM yyyy}.";

                    await _notificationService.CreateAsync(student.UserID, message, "new_assessment");
                }
            }

            TempData["SuccessMessage"] = "Assessment created.";
            return RedirectToAction(nameof(ManageAssessments));
        }

        // Edit Assessment (GET)
        public async Task<IActionResult> EditAssessment(int? id)
        {
            if (id == null) return NotFound();
            var assessment = await _context.Assessments
                .Include(a => a.StudentModule)
                .FirstOrDefaultAsync(a => a.AssessmentID == id && !a.IsDeleted);
            if (assessment == null) return NotFound();

            // Check ownership via lecturer module
            var userId = GetCurrentUserId();
            if (assessment.StudentModule.LecturerModule.UserID != userId)
                return Unauthorized();

            var studentModules = _context.StudentModules
                .Where(sm => sm.LecturerModule.UserID == userId && sm.StudModStatus == StudModStatus.Active)
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Select(sm => new { sm.StudentModuleID, DisplayName = sm.Student.Name + " " + sm.Student.Surname + " - " + sm.LecturerModule.Module.ModuleName })
                .ToList();

            ViewData["StudentModuleID"] = new SelectList(studentModules, "StudentModuleID", "DisplayName", assessment.StudentModuleID);
            ViewData["AssessmentTypeID"] = new SelectList(_context.AssessmentTypes.Where(t => !t.IsDeleted), "AssessmentTypeID", "AssessmentTypeDescription", assessment.AssessmentTypeID);

            return View(assessment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessment(int id, [Bind("AssessmentID,StudentModuleID,AssessmentTypeID,DueDate")] Assessment model)
        {
            if (id != model.AssessmentID) return NotFound();
            var userId = GetCurrentUserId();

            var existing = await _context.Assessments
                .Include(a => a.StudentModule)
                .FirstOrDefaultAsync(a => a.AssessmentID == id && !a.IsDeleted);
            if (existing == null || existing.StudentModule.LecturerModule.UserID != userId)
                return Unauthorized();

            if (ModelState.IsValid)
            {
                existing.StudentModuleID = model.StudentModuleID;
                existing.AssessmentTypeID = model.AssessmentTypeID;
                existing.DueDate = model.DueDate;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageAssessments));
            }
            // Re-populate dropdowns
            return View(model);
        }

        // Soft delete
        public async Task<IActionResult> DeleteAssessment(int? id)
        {
            if (id == null) return NotFound();
            var assessment = await _context.Assessments
                .Include(a => a.StudentModule)
                .FirstOrDefaultAsync(a => a.AssessmentID == id && !a.IsDeleted);
            if (assessment == null) return NotFound();
            var userId = GetCurrentUserId();
            if (assessment.StudentModule.LecturerModule.UserID != userId)
                return Unauthorized();
            return View(assessment);
        }

        [HttpPost, ActionName("DeleteAssessment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessmentConfirmed(int id)
        {
            var assessment = await _context.Assessments.FindAsync(id);
            if (assessment != null && !assessment.IsDeleted)
            {
                assessment.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageAssessments));
        }

        // ===== STUDENT LOOKUP & REPORT =====
        // GET: LookupStudents – list all students in my active modules
        public async Task<IActionResult> LookupStudents()
        {
            var userId = GetCurrentUserId();
            var studentUsers = await _context.StudentModules
                .Where(sm => sm.LecturerModule.UserID == userId && sm.StudModStatus == StudModStatus.Active)
                .Select(sm => sm.Student)
                .Distinct()
                .OrderBy(s => s.Surname)
                .ToListAsync();
            return View(studentUsers);
        }

        // GET: StudentDetails/5 – show all assessments for this student in my modules
        public async Task<IActionResult> StudentDetails(int studentId)
        {
            var userId = GetCurrentUserId();
            var student = await _context.Accounts.FindAsync(studentId);
            if (student == null) return NotFound();

            var assessments = await _context.Assessments
                .Where(a => !a.IsDeleted && a.StudentModule.UserID == studentId
                    && a.StudentModule.LecturerModule.UserID == userId)
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.LecturerModule)
                        .ThenInclude(lm => lm.Module)
                .Include(a => a.AssessmentType)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            ViewBag.Student = student;
            return View(assessments);
        }

        // ===== ASSESSMENT REPORT (lecturer’s own) =====
        public async Task<IActionResult> AssessmentReport(AssessmentStatus? status, int? typeId, DateTime? fromDate, DateTime? toDate)
        {
            var userId = GetCurrentUserId();
            var lecturerModuleIds = await _context.LecturerModules
                .Where(lm => lm.UserID == userId && lm.ModLecturerStatus == ModLecturerStatus.Active)
                .Select(lm => lm.LecturerModuleID)
                .ToListAsync();

            var query = _context.Assessments
                .Where(a => !a.IsDeleted && lecturerModuleIds.Contains(a.StudentModule.LecturerModuleID))
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
    }
}