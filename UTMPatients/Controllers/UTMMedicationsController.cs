using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTMPatients.Models;

namespace UTMPatients.Controllers
{
    public class UTMMedicationsController : Controller
    {
        private readonly PatientsContext _context;

        public UTMMedicationsController(PatientsContext context)
        {
            _context = context;
        }

        // GET: UTMMedications
        public async Task<IActionResult> Index(int? medId, string medName)
        {
            if (medId.HasValue)
            {
                HttpContext.Session.SetString("MedicationTypeId", medId.ToString());
            }
            else if (Request.Query["MedicationTypeId"].Any())
            {
                return RedirectToAction("Index", "UTMMedicationTypes", new { message = "Please select a medication type" });
            }
            var patientsContext = _context.Medication.Include(m => m.ConcentrationCodeNavigation)
                                         .Include(m => m.DispensingCodeNavigation)
                                         .Include(m => m.MedicationType)
                                         .Where(m => m.MedicationTypeId == Convert.ToInt32(HttpContext.Session.GetString("MedicationTypeId")))
                                         .OrderBy(n => n.Name)
                                         .ThenBy(c => c.Concentration);

            TempData["MedicationTypeId"] = medId;

            if (medName == null)
            {
                TempData["MedicationName"] = patientsContext.FirstOrDefault()?.MedicationType.Name;
            }
            else
            {
                TempData["MedicationName"] = medName;
            }

            return View(await patientsContext.ToListAsync());
        }


        // GET: UTMMedications/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medication = await _context.Medication
                .Include(m => m.ConcentrationCodeNavigation)
                .Include(m => m.DispensingCodeNavigation)
                .Include(m => m.MedicationType)
                .FirstOrDefaultAsync(m => m.Din == id);
            if (medication == null)
            {
                return NotFound();
            }

            return View(medication);
        }

        // GET: UTMMedications/Create
        public IActionResult Create(string medName, int medTypeId)
        {
            ViewData["MedicationName"] = medName;
            ViewData["ConcentrationCode"] = new SelectList(_context.ConcentrationUnit.OrderBy(x => x.ConcentrationCode), "ConcentrationCode", "ConcentrationCode");
            ViewData["DispensingCode"] = new SelectList(_context.DispensingUnit.OrderBy(x => x.DispensingCode), "DispensingCode", "DispensingCode");
            ViewData["MedicationTypeId"] = medTypeId;
            return View();
        }

        // POST: UTMMedications/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Din,Name,Image,MedicationTypeId,DispensingCode,Concentration,ConcentrationCode")] Medication medication)
        {
            if (ModelState.IsValid)
            {
                var patientsContext = _context.Medication.Include(c => c.ConcentrationCodeNavigation).Include(c => c.MedicationType).Where(x => x.MedicationTypeId == medication.MedicationTypeId);
                ViewData["MedicationName"] = patientsContext.FirstOrDefault()?.MedicationType.Name;
                if (patientsContext.Any(n => n.Name == medication.Name))
                {
                    ViewData["Error"] = "Medication name already exists.";
                }
                else if (patientsContext.Any(c => c.Concentration == medication.Concentration && c.ConcentrationCode == medication.ConcentrationCode))
                {
                    ViewData["Error"] = "Medication concentration and code already exists.";
                }
                else
                {
                    _context.Add(medication);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { medId = medication.MedicationTypeId });
                }
            }
            ViewData["ConcentrationCode"] = new SelectList(_context.ConcentrationUnit, "ConcentrationCode", "ConcentrationCode", medication.ConcentrationCode);
            ViewData["DispensingCode"] = new SelectList(_context.DispensingUnit, "DispensingCode", "DispensingCode", medication.DispensingCode);
            ViewData["MedicationTypeId"] = new SelectList(_context.MedicationType, "MedicationTypeId", "Name", medication.MedicationTypeId);
            return View(medication);
        }

        // GET: UTMMedications/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medication = await _context.Medication.Include(m => m.MedicationType).FirstOrDefaultAsync(m => m.Din == id);
            if (medication == null)
            {
                return NotFound();
            }
            ViewData["ConcentrationCode"] = new SelectList(_context.ConcentrationUnit, "ConcentrationCode", "ConcentrationCode", medication.ConcentrationCode);
            ViewData["DispensingCode"] = new SelectList(_context.DispensingUnit, "DispensingCode", "DispensingCode", medication.DispensingCode);
            ViewData["MedicationTypeId"] = new SelectList(_context.MedicationType, "MedicationTypeId", "Name", medication.MedicationTypeId);
            return View(medication);
        }

        // POST: UTMMedications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Din,Name,Image,MedicationTypeId,DispensingCode,Concentration,ConcentrationCode")] Medication medication)
        {
            if (id != medication.Din)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medication);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicationExists(medication.Din))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { medId = medication.MedicationTypeId });
            }
            ViewData["ConcentrationCode"] = new SelectList(_context.ConcentrationUnit, "ConcentrationCode", "ConcentrationCode", medication.ConcentrationCode);
            ViewData["DispensingCode"] = new SelectList(_context.DispensingUnit, "DispensingCode", "DispensingCode", medication.DispensingCode);
            ViewData["MedicationTypeId"] = new SelectList(_context.MedicationType, "MedicationTypeId", "Name", medication.MedicationTypeId);
            return View(medication);
        }

        // GET: UTMMedications/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medication = await _context.Medication
                .Include(m => m.ConcentrationCodeNavigation)
                .Include(m => m.DispensingCodeNavigation)
                .Include(m => m.MedicationType)
                .FirstOrDefaultAsync(m => m.Din == id);
            if (medication == null)
            {
                return NotFound();
            }

            return View(medication);
        }

        // POST: UTMMedications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var medication = await _context.Medication.FindAsync(id);
            _context.Medication.Remove(medication);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MedicationExists(string id)
        {
            return _context.Medication.Any(e => e.Din == id);
        }
    }
}
