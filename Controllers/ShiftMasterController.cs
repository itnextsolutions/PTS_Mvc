using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MVC_BugTracker.Data;
using MVC_BugTracker.Models;
using MVC_BugTracker.Services.Interfaces;
using MVC_BugTracker.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MVC_BugTracker.Models.ViewModels;
using MVC_BugTracker.Extensions;

namespace MVC_BugTracker.Controllers
{
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class ShiftMasterController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ShiftMasterController(ApplicationDbContext context)
        {
            _context = context;
           
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> AllShift()
        {
            int companyid = User.Identity.GetCompanyId().Value;
            var shifts = await _context.RotationShift.Where(x=>x.CompanyId==companyid).ToListAsync();
            return View(shifts);


        }

        public async Task<IActionResult> Create()
        {
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Shift_type,StartTime,EndTime")] RotationShift shift)
        {
            if (ModelState.IsValid)
            {
                int companyid = User.Identity.GetCompanyId().Value;
                shift.CompanyId = companyid;
                _context.Add(shift);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AllShift));
            }
            return View(shift);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.RotationShift.FindAsync(id);
            if (shift == null)
            {
                return NotFound();
            }
            return PartialView(shift);
        }

        // POST: Shift/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Shift_type,StartTime,EndTime,CompanyId")] RotationShift shift)
        {
            if (id != shift.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shift);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShiftExists(shift.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(AllShift));
            }
            return View(shift);
        }

        private bool ShiftExists(int id)
        {
            return _context.RotationShift.Any(e => e.Id == id);
        }

        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rotationShift = _context.RotationShift.FirstOrDefault(rs => rs.Id == id);

            if (rotationShift == null)
            {
                return NotFound();
            }

            return PartialView(rotationShift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var rotationShift = _context.RotationShift.FirstOrDefault(rs => rs.Id == id);

            if (rotationShift == null)
            {
                return NotFound();
            }

            _context.RotationShift.Remove(rotationShift);
            _context.SaveChanges();
            return RedirectToAction(nameof(AllShift));
        }
    }
}
