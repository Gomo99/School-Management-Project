// Controllers/ParentController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.Status;
using System.Security.Claims;

namespace SchoolProject.Controllers
{
    [Authorize(Roles = "Parent")]
    public class ParentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParentController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue("UserID"));
        }

        // Dashboard: list all linked children
        public async Task<IActionResult> Dashboard()
        {
            int parentId = GetCurrentUserId();

            var children = await _context.StudentParentLinks
                .Where(l => l.ParentUserId == parentId)
                .Include(l => l.Student)
                .Select(l => new
                {
                    l.Student.UserID,
                    l.Student.Name,
                    l.Student.Surname,
                    l.Student.Title
                })
                .ToListAsync();

            ViewBag.Children = children;
            return View();
        }

        // Show enrolled modules for a specific child
        public async Task<IActionResult> ChildModules(int studentId)
        {
            int parentId = GetCurrentUserId();

            // Verify this child belongs to the logged‑in parent
            var link = await _context.StudentParentLinks
                .FirstOrDefaultAsync(l => l.ParentUserId == parentId && l.StudentUserId == studentId);

            if (link == null)
                return Unauthorized("You are not authorized to view this student.");

            var modules = await _context.StudentModules
                .Where(sm => sm.UserID == studentId && sm.StudModStatus == StudModStatus.Active)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Lecturer)
                .ToListAsync();

            ViewBag.Student = await _context.Accounts.FindAsync(studentId);
            return View(modules);
        }

        // Show assessments for a specific child in a specific module
        public async Task<IActionResult> ChildAssessments(int studentId, int studentModuleId)
        {
            int parentId = GetCurrentUserId();

            var link = await _context.StudentParentLinks
                .FirstOrDefaultAsync(l => l.ParentUserId == parentId && l.StudentUserId == studentId);

            if (link == null)
                return Unauthorized();

            var assessments = await _context.Assessments
                .Where(a => a.StudentModuleID == studentModuleId && !a.IsDeleted)
                .Include(a => a.AssessmentType)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            ViewBag.Student = await _context.Accounts.FindAsync(studentId);
            var sm = await _context.StudentModules
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .FirstOrDefaultAsync(sm => sm.StudentModuleID == studentModuleId);
            ViewBag.ModuleName = sm?.LecturerModule?.Module?.ModuleName ?? "Unknown Module";

            return View(assessments);
        }
    }
}