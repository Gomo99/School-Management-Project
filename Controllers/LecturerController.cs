using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;

namespace SchoolProject.Controllers
{
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LecturerController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Lecturer")]
        public IActionResult Dashboard()
        {
            return View();
        }


        public async Task<IActionResult> ManageAssessmentType()
        {
            // Only show active types
            var activeTypes = await _context.assessmentTypes
                .Where(a => a.AssessmentTypeStatus == AssessmentTypeStatus.Active)
                .ToListAsync();

            return View(activeTypes);
        }

        // GET: AssessmentTypes/Details/5
        public async Task<IActionResult> DetailsAssessmentType(int? id)
        {
            if (id == null) return NotFound();

            var type = await _context.assessmentTypes
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

            var type = await _context.assessmentTypes.FindAsync(id);
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
                    var existing = await _context.assessmentTypes.FindAsync(id);
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

            var type = await _context.assessmentTypes
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
            var type = await _context.assessmentTypes.FindAsync(id);
            if (type != null && type.AssessmentTypeStatus == AssessmentTypeStatus.Active)
            {
                type.AssessmentTypeStatus = AssessmentTypeStatus.Inactive;
                _context.Update(type);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageAssessmentType));
        }

        private bool AssessmentTypeExists(int id)
        {
            return _context.assessmentTypes.Any(e => e.AssessmentTypeID == id);
        }

    }
}
