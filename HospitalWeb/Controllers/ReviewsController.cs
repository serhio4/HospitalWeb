using HospitalWeb.Data;
using HospitalWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HospitalWeb.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reviews
        public async Task<IActionResult> Index()
        {
            // Includem Programarea, si din Programare includem Doctorul si Pacientul
            var hospitalDbContext = _context.Reviews
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient);

            return View(await hospitalDbContext.ToListAsync());
        }

        // GET: Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Appointment)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // GET: Reviews/Create
        public async Task<IActionResult> Create(int? appointmentId)
        {
            if (appointmentId == null)
            {
                return RedirectToAction("Index", "Appointments");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            // Aici trimitem textul frumos catre View
            ViewData["AppointmentInfo"] = $"Dr. {appointment.Doctor.FullName} - {appointment.AppointmentDate:dd MMM yyyy}";

            // Aici setam ID-ul ca sa fie pre-completat in input-ul ascuns
            var review = new Review { AppointmentId = appointment.Id };

            return View(review);
        }

        // POST: Reviews/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Rating,Comment,AppointmentId")] Review review)
        {
            if (ModelState.IsValid)
            {
                _context.Add(review);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "Id", "Id", review.AppointmentId);
            return View(review);
        }

        // GET: Reviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var review = await _context.Reviews
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient) // Avem nevoie de pacient ca sa verificam cine e
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null) return NotFound();

            // Daca NU esti Admin SI nici NU esti proprietarul recenziei -> Respins
            if (!User.IsInRole("Admin") && review.Appointment.Patient.Email != User.Identity.Name)
            {
                return Forbid(); // Sau return Unauthorized();
            }
            // ------------------

            return View(review);
        }

        // POST: Reviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Rating,Comment,AppointmentId")] Review review)
        {
            if (id != review.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(review);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "Id", "Id", review.AppointmentId);
            return View(review);
        }

        // GET: Reviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var review = await _context.Reviews
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient) // Incarcam pacientul
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null) return NotFound();

            // Aceeasi verificare ca la Edit
            if (!User.IsInRole("Admin") && review.Appointment.Patient.Email != User.Identity.Name)
            {
                return Forbid();
            }
            // ------------------

            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.Id == id);
        }
    }
}
