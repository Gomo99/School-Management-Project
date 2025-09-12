using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using System.Security.Claims;

namespace SchoolProject.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LecturerController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        public IActionResult Dashboard()
        {
            var currentUserName = User.Identity.Name;
            return View();
        }


        public async Task<IActionResult> ListModules()
        {
            var userId = GetCurrentUserId();

            var lecturerModules = await _context.LecturerModules
                .Include(lm => lm.Module)
                .Where(lm => lm.UserID == userId && lm.ModLecturerStatus == ModLecturerStatus.Active)
                .ToListAsync();

            return View(lecturerModules);
        }

    


        public async Task<IActionResult> ManageAssessmentType()
        {
            // Only show active types
            var activeTypes = await _context.AssessmentTypes
                .Where(a => a.AssessmentTypeStatus == AssessmentTypeStatus.Active)
                .ToListAsync();

            return View(activeTypes);
        }

        // GET: AssessmentTypes/Details/5
        public async Task<IActionResult> DetailsAssessmentType(int? id)
        {
            if (id == null) return NotFound();

            var type = await _context.AssessmentTypes
                .FirstOrDefaultAsync(m => m.AssessmentTypeID == id);

            if (type == null || type.AssessmentTypeStatus == AssessmentTypeStatus.Inactive)
                return NotFound();

            return View(type);
        }

        // GET: AssessmentTypes/Create
        public IActionResult AddAssessmentType()
        {
            return View();
        }

        // POST: AssessmentTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssessmentType([Bind("AssessmentTypeDescription")] AssessmentType type)
        {
            if (ModelState.IsValid)
            {
                type.AssessmentTypeStatus = AssessmentTypeStatus.Active;
                _context.Add(type);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageAssessmentType));
            }
            return View(type);
        }

        // GET: AssessmentTypes/Edit/5
        public async Task<IActionResult> EditAssessmentType(int? id)
        {
            if (id == null) return NotFound();

            var type = await _context.AssessmentTypes.FindAsync(id);
            if (type == null || type.AssessmentTypeStatus == AssessmentTypeStatus.Inactive)
                return NotFound();

            return View(type);
        }

        // POST: AssessmentTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessmentType(int id, [Bind("AssessmentTypeID,AssessmentTypeDescription")] AssessmentType type)
        {
            if (id != type.AssessmentTypeID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.AssessmentTypes.FindAsync(id);
                    if (existing == null || existing.AssessmentTypeStatus == AssessmentTypeStatus.Inactive)
                        return NotFound();

                    existing.AssessmentTypeDescription = type.AssessmentTypeDescription;
                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssessmentTypeExists(type.AssessmentTypeID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ManageAssessmentType));
            }
            return View(type);
        }

        // GET: AssessmentTypes/Delete/5
        public async Task<IActionResult> DeleteAssessmentType(int? id)
        {
            if (id == null) return NotFound();

            var type = await _context.AssessmentTypes
                .FirstOrDefaultAsync(m => m.AssessmentTypeID == id);

            if (type == null || type.AssessmentTypeStatus == AssessmentTypeStatus.Inactive)
                return NotFound();

            return View(type);
        }

        // POST: AssessmentTypes/Delete/5 (Soft Delete)
        [HttpPost, ActionName("DeleteAssessmentType")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var type = await _context.AssessmentTypes.FindAsync(id);
            if (type != null && type.AssessmentTypeStatus == AssessmentTypeStatus.Active)
            {
                type.AssessmentTypeStatus = AssessmentTypeStatus.Inactive;
                _context.Update(type);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageAssessmentType));
        }







        public IActionResult ManageAssessments()
        {
            var assessments = _context.Assessments
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.Student)
                .Include(a => a.StudentModule)
                    .ThenInclude(sm => sm.LecturerModule)
                        .ThenInclude(lm => lm.Module)
                .Include(a => a.AssessmentType)
                .ToList();

            return View(assessments);
        }


        // GET: Add Assessment
        // GET: Add Assessment
        public IActionResult AddAssessment()
        {
            var studentModules = _context.StudentModules
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module) // Module comes through LecturerModule
                .Select(sm => new
                {
                    sm.StudentModuleID,
                    DisplayName = sm.Student.Name + " " + sm.Student.Surname + " - " + sm.LecturerModule.Module.ModuleName
                })
                .ToList();

            ViewData["StudentModuleID"] = new SelectList(studentModules, "StudentModuleID", "DisplayName");

            ViewData["AssessmentTypeID"] = new SelectList(
                _context.AssessmentTypes.Where(t => t.AssessmentTypeStatus == AssessmentTypeStatus.Active),
                "AssessmentTypeID",
                "AssessmentTypeDescription"
            );

            return View();
        }


        // POST: Add Assessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssessment([Bind("StudentModuleID,AssessmentTypeID,DueDate")] Assessment assessment)
        {
            
                assessment.AssessmentStatus = AssessmentStatus.Active;
                _context.Assessments.Add(assessment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ManageAssessments));
            

            var studentModules = _context.StudentModules
       .Include(sm => sm.Student)
       .Include(sm => sm.LecturerModule)
           .ThenInclude(lm => lm.Module)
       .Select(sm => new
       {
           sm.StudentModuleID,
           DisplayName = sm.Student.Name + " " + sm.Student.Surname + " - " + sm.LecturerModule.Module.ModuleName
       })
       .ToList();

            ViewData["StudentModuleID"] = new SelectList(studentModules, "StudentModuleID", "DisplayName", assessment.StudentModuleID);

            ViewData["AssessmentTypeID"] = new SelectList(
                _context.AssessmentTypes.Where(t => t.AssessmentTypeStatus == AssessmentTypeStatus.Active),
                "AssessmentTypeID",
                "AssessmentTypeDescription",
                assessment.AssessmentTypeID
            );

            return View(assessment);
        }

        // GET: Edit Assessment
        // GET: Edit Assessment
        public async Task<IActionResult> EditAssessment(int? id)
        {
            if (id == null) return NotFound();

            var assessment = await _context.Assessments.FindAsync(id);
            if (assessment == null || assessment.AssessmentStatus == AssessmentStatus.Inactive) return NotFound();

            // Prepare StudentModule dropdown with Student Name + Module Name
            var studentModules = _context.StudentModules
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Select(sm => new
                {
                    sm.StudentModuleID,
                    DisplayName = sm.Student.Name + " " + sm.Student.Surname + " - " + sm.LecturerModule.Module.ModuleName
                })
                .ToList();

            ViewData["StudentModuleID"] = new SelectList(studentModules, "StudentModuleID", "DisplayName", assessment.StudentModuleID);

            // AssessmentType dropdown
            ViewData["AssessmentTypeID"] = new SelectList(
                _context.AssessmentTypes.Where(t => t.AssessmentTypeStatus == AssessmentTypeStatus.Active),
                "AssessmentTypeID",
                "AssessmentTypeDescription",
                assessment.AssessmentTypeID
            );

            return View(assessment);
        }

        // POST: Edit Assessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessment(int id, [Bind("AssessmentID,StudentModuleID,AssessmentTypeID,DueDate")] Assessment assessment)
        {
            if (id != assessment.AssessmentID) return NotFound();

           
                try
                {
                    var existing = await _context.Assessments.FindAsync(id);
                    if (existing == null || existing.AssessmentStatus == AssessmentStatus.Inactive) return NotFound();

                    existing.StudentModuleID = assessment.StudentModuleID;
                    existing.AssessmentTypeID = assessment.AssessmentTypeID;
                    existing.DueDate = assessment.DueDate;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssessmentExists(assessment.AssessmentID)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(ManageAssessments));
            

            // Re-populate dropdowns if ModelState is invalid
            var studentModules = _context.StudentModules
                .Include(sm => sm.Student)
                .Include(sm => sm.LecturerModule)
                    .ThenInclude(lm => lm.Module)
                .Select(sm => new
                {
                    sm.StudentModuleID,
                    DisplayName = sm.Student.Name + " " + sm.Student.Surname + " - " + sm.LecturerModule.Module.ModuleName
                })
                .ToList();

            ViewData["StudentModuleID"] = new SelectList(studentModules, "StudentModuleID", "DisplayName", assessment.StudentModuleID);

            ViewData["AssessmentTypeID"] = new SelectList(
                _context.AssessmentTypes.Where(t => t.AssessmentTypeStatus == AssessmentTypeStatus.Active),
                "AssessmentTypeID",
                "AssessmentTypeDescription",
                assessment.AssessmentTypeID
            );

            return View(assessment);
        }

        // GET: Delete Assessment
        public async Task<IActionResult> DeleteAssessment(int? id)
        {
            if (id == null) return NotFound();

            var assessment = await _context.Assessments
                .Include(a => a.AssessmentType)
                .Include(a => a.StudentModule)
                .FirstOrDefaultAsync(m => m.AssessmentID == id);

            if (assessment == null || assessment.AssessmentStatus == AssessmentStatus.Inactive) return NotFound();

            return View(assessment);
        }

        // POST: Delete Assessment (Soft Delete)
        [HttpPost, ActionName("DeleteAssessment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessmentConfirmed(int id)
        {
            var assessment = await _context.Assessments.FindAsync(id);
            if (assessment != null && assessment.AssessmentStatus == AssessmentStatus.Active)
            {
                assessment.AssessmentStatus = AssessmentStatus.Inactive;
                _context.Update(assessment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageAssessments));
        }



        // GET: Lecturer/LookupStudents
        public async Task<IActionResult> LookupStudents()
        {
            var students = await _context.Accounts
                .Where(a => a.Role == UserRole.Student && a.UserStatus == UserStatus.Active)
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(students);
        }







        // Helper
        private bool AssessmentExists(int id)
        {
            return _context.Assessments.Any(e => e.AssessmentID == id);
        }





        
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue("UserID"));
        }








        private bool AssessmentTypeExists(int id)
        {
            return _context.AssessmentTypes.Any(e => e.AssessmentTypeID == id);
        }

    }
}
