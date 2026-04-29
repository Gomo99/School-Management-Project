using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using System.Security.Claims;

namespace SchoolProject.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper to get current user ID
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue("UserID"));
        }

        // GET: Student/Dashboard – Show enrolled modules
        public async Task<IActionResult> Dashboard()
        {
            int userId = GetCurrentUserId();

            // Get active student modules with related data
            var enrolledModules = await _context.StudentModules
                .Where(sm => sm.UserID == userId && sm.StudModStatus == StudModStatus.Active)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Lecturer)   // lecturer info
                .ToListAsync();

            ViewBag.StudentName = User.Identity.Name;
            return View(enrolledModules);
        }

        // GET: Student/ViewAssessments/5 (StudentModuleID)
        public async Task<IActionResult> ViewAssessments(int id)
        {
            int userId = GetCurrentUserId();

            // Ensure this student module belongs to the current student and is active
            var studentModule = await _context.StudentModules
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == id
                    && sm.UserID == userId
                    && sm.StudModStatus == StudModStatus.Active);

            if (studentModule == null)
                return NotFound("Module not found or not enrolled.");

            // Get assessments (excluding soft‑deleted) for this student module
            var assessments = await _context.Assessments
                .Where(a => a.StudentModuleID == id && !a.IsDeleted)
                .Include(a => a.AssessmentType)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            ViewBag.ModuleName = studentModule.LecturerModule.Module.ModuleName;
            ViewBag.StudentModuleID = studentModule.StudentModuleID;

            return View(assessments);
        }

        // GET: Student/UpdateAssessmentStatus/5 (AssessmentID)
        public async Task<IActionResult> UpdateAssessmentStatus(int id)
        {
            int userId = GetCurrentUserId();

            var assessment = await _context.Assessments
                .Include(a => a.StudentModule)
                .FirstOrDefaultAsync(a => a.AssessmentID == id && !a.IsDeleted);

            if (assessment == null)
                return NotFound("Assessment not found.");

            // Security: ensure the assessment belongs to the current student
            if (assessment.StudentModule.UserID != userId)
                return Unauthorized("You can only update your own assessments.");

            ViewBag.AssessmentID = assessment.AssessmentID;
            ViewBag.CurrentStatus = assessment.AssessmentStatus;
            ViewBag.DueDate = assessment.DueDate;
            ViewBag.ModuleName = (await _context.StudentModules
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == assessment.StudentModuleID))
                ?.LecturerModule?.Module?.ModuleName ?? "";

            return View(assessment);
        }

        // POST: Student/UpdateAssessmentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssessmentStatus(int id, AssessmentStatus newStatus, DateTime? newDueDate)
        {
            int userId = GetCurrentUserId();

            var assessment = await _context.Assessments
                .Include(a => a.StudentModule)
                .FirstOrDefaultAsync(a => a.AssessmentID == id && !a.IsDeleted);

            if (assessment == null)
                return NotFound();

            if (assessment.StudentModule.UserID != userId)
                return Unauthorized();

            // Validate the new status
            if (newStatus == AssessmentStatus.Rescheduled && (!newDueDate.HasValue || newDueDate < DateTime.Today))
            {
                TempData["ErrorMessage"] = "Rescheduled assessment must have a future date.";
                return RedirectToAction("UpdateAssessmentStatus", new { id });
            }

            assessment.AssessmentStatus = newStatus;
            if (newStatus == AssessmentStatus.Rescheduled && newDueDate.HasValue)
            {
                assessment.DueDate = newDueDate.Value;
            }

            _context.Update(assessment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Assessment updated successfully.";
            return RedirectToAction("ViewAssessments", new { id = assessment.StudentModuleID });
        }

        // GET: Student/GetNotifications – returns JSON for dropdown
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            int userId = GetCurrentUserId();
            var notifs = await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new {
                    n.NotificationID,
                    n.Message,
                    n.CreatedAt,
                    n.IsRead
                })
                .ToListAsync();
            return Json(notifs);
        }

        // POST: Student/MarkNotificationRead
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            int userId = GetCurrentUserId();
            var notif = await _context.Notifications.FindAsync(id);
            if (notif != null && notif.UserID == userId)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // POST: Student/MarkAllNotificationsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            int userId = GetCurrentUserId();
            var unread = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
            return Ok();
        }







    }
}