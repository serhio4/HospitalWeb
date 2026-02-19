using HospitalWeb.Data;
using HospitalWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalWeb.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Appointments
        [Authorize] // Trebuie sa fii logat ca sa vezi lista
        public async Task<IActionResult> Index()
        {
            var currentEmail = User.Identity.Name;

            // 1. Pornim cu interogarea de baza (Toate programarile + Relatii)
            var appointmentsQuery = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Review)
                .AsQueryable(); 

            // 2. APLICAM FILTRE IN FUNCTIE DE ROL

            if (User.IsInRole("Admin"))
            {
                // Adminul vede TOT -> Nu aplicam niciun filtru
            }
            else if (User.IsInRole("Doctor"))
            {
                // Doctorul vede doar programarile unde EL este medicul
                appointmentsQuery = appointmentsQuery.Where(a => a.Doctor.Email == currentEmail);
            }
            else
            {
                // Orice alt utilizator (Pacient) vede doar programarile LUI
                appointmentsQuery = appointmentsQuery.Where(a => a.Patient.Email == currentEmail);
            }

            // 3. Executam interogarea si ordonam descrescator (cele mai noi primele)
            var finalStateMachine = await appointmentsQuery
                                          .OrderByDescending(a => a.AppointmentDate)
                                          .ToListAsync();

            return View(finalStateMachine);
        }

        // Schimbarea Statusului (Apelata din butoanele din Index)
        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")] // Doar doctorii si adminii confirma programari
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, AppointmentStatus status)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Actualizam statusul
            appointment.Status = status;
            _context.Update(appointment);
            await _context.SaveChangesAsync();

            // Ne intoarcem la lista
            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Create
        [Authorize]
        public IActionResult Create()
        {
            // 1. Verificare existenta profil pacient (Doar pt pacienti)
            if (!User.IsInRole("Admin"))
            {
                var currentUserEmail = User.Identity.Name;
                var patientRecord = _context.Patients.FirstOrDefault(p => p.Email == currentUserEmail);

                if (patientRecord == null)
                {
                    // Cream pacientul automat daca nu exista (Auto-Register)
                    patientRecord = new Patient
                    {
                        FullName = currentUserEmail,
                        Email = currentUserEmail,
                        PhoneNumber = "-",
                        CNP = "-"
                    };
                    _context.Patients.Add(patientRecord);
                    _context.SaveChanges();
                }
            }

            // 2. MODIFICAREA PENTRU DROPDOWN CU SPECIALIZARE (Pentru toata lumea)
            var doctorDisplayList = _context.Doctors
                .Include(d => d.Department)
                .Select(d => new
                {
                    Id = d.Id,
                    DisplayText = d.FullName + " (" + d.Department.Name + ")"
                })
                .ToList();

            ViewData["DoctorId"] = new SelectList(doctorDisplayList, "Id", "DisplayText");

            // --- 3.POPULARE DROPDOWN PACIENTI (DOAR PENTRU ADMIN) ---
            if (User.IsInRole("Admin"))
            {
                var patientDisplayList = _context.Patients
                    .Select(p => new
                    {
                        Id = p.Id,
                        // Afisam numele si email-ul ca sa fie usor de identificat
                        DisplayPatient = p.FullName + " (" + p.Email + ")"
                    })
                    .ToList();

                ViewData["PatientId"] = new SelectList(patientDisplayList, "Id", "DisplayPatient");
            }
            // ------------------------------------------------------------------

            return View();
        }

        // GET: Appointments/Details/5
        [Authorize] // Oricine e logat poate incerca sa vada, dar filtram mai jos
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }


            // 1. Daca e PACIENT -> Verificam daca e programarea LUI
            if (!User.IsInRole("Admin") && !User.IsInRole("Doctor"))
            {
                // Daca emailul pacientului din programare NU e acelasi cu cel logat
                if (appointment.Patient?.Email != User.Identity.Name)
                {
                    // Il trimitem inapoi la lista sau ii dam eroare
                    return Unauthorized();
                }
            }

            // 2. Daca e DOCTOR -> Verificam daca e pacientul LUI
            if (User.IsInRole("Doctor"))
            {
                if (appointment.Doctor?.Email != User.Identity.Name)
                {
                    return Unauthorized();
                }
            }

            return View(appointment);
        }

        // POST: Appointments/Cancel/5
        [HttpPost]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);

            // Verificam: Exista? E al meu? E in viitor?
            if (appointment == null || appointment.Patient.Email != User.Identity.Name)
            {
                return BadRequest();
            }

            if (appointment.AppointmentDate < DateTime.Now)
            {
                return BadRequest("Nu poți anula o programare din trecut.");
            }

            appointment.Status = AppointmentStatus.Cancelled; 
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Oricine logat
        public async Task<IActionResult> Create([Bind("Id,AppointmentDate,DoctorId,PatientId")] Appointment appointment)
        {
            if (appointment.AppointmentDate < DateTime.Now)
            {
                ModelState.AddModelError("AppointmentDate", "Nu te poți programa în trecut!");
            }

            // --- VALIDARE: PROGRAMARE IN WEEKEND  ---
            if (appointment.AppointmentDate.DayOfWeek == DayOfWeek.Saturday ||
                appointment.AppointmentDate.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("AppointmentDate", "Clinica este închisă în weekend.");
            }

            // 2. Verificam orele (09:00 - 17:00)
            if (appointment.AppointmentDate.Hour < 9 || appointment.AppointmentDate.Hour >= 17)
            {
                ModelState.AddModelError("AppointmentDate", "Programările se fac doar între orele 09:00 și 17:00.");
            }

            var conflict = await _context.Appointments
    .AnyAsync(a => a.DoctorId == appointment.DoctorId
                   && a.AppointmentDate == appointment.AppointmentDate
                   && a.Status != AppointmentStatus.Cancelled);

            if (conflict)
            {
                ModelState.AddModelError("AppointmentDate", "Doctorul este deja ocupat la această oră. Alege altă oră.");
            }
            // 1. LOGICA AUTOMATA PENTRU PACIENTI
            if (!User.IsInRole("Admin"))
            {
                var currentUserEmail = User.Identity.Name;

                // Cautam daca exista deja profilul
                var patientRecord = await _context.Patients.FirstOrDefaultAsync(p => p.Email == currentUserEmail);

                // --- Daca nu exista, il cream PE LOC ---
                if (patientRecord == null)
                {
                    patientRecord = new Patient
                    {
                        FullName = currentUserEmail, // Folosim mailul ca nume temporar
                        Email = currentUserEmail,
                        PhoneNumber = "-", // Completam cu o valoare implicita ca sa treaca validarea
                        CNP = "-"          // Completam cu o valoare implicita
                    };

                    _context.Patients.Add(patientRecord);
                    await _context.SaveChangesAsync(); // Il salvam in baza de date
                }
                // -----------------------------------------------------------

                // Acum sigur avem un ID valid
                appointment.PatientId = patientRecord.Id;

                // Eliminam erorile de validare
                ModelState.Remove("PatientId");
                ModelState.Remove("Patient");
            }

            // 2. SALVAREA PROPRIU-ZISA
            if (ModelState.IsValid)
            {
                // Setam statusul implicit
                appointment.Status = AppointmentStatus.Pending;

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Daca ceva a mers prost, reincarcam pagina
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "Id", "FullName", appointment.DoctorId);

            // Incarcam lista de pacienti doar daca e Admin
            if (User.IsInRole("Admin"))
            {
                ViewData["PatientId"] = new SelectList(_context.Patients, "Id", "FullName", appointment.PatientId);
            }

            return View(appointment);
        }


        // GET: Appointments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // --- Folosim Include ca sa aducem Numele Doctorului si Pacientului ---
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)  // <--- Asta incarca numele doctorului
                .Include(a => a.Patient) // <--- Asta incarca numele pacientului
                .FirstOrDefaultAsync(a => a.Id == id);
            // ----------------------------------------------------------------------------------

            if (appointment == null)
            {
                return NotFound();
            }

            // Verificare suplimentara: Un doctor nu poate edita programarea altui doctor
            if (User.IsInRole("Doctor") && appointment.Doctor?.Email != User.Identity.Name)
            {
                return Forbid();
            }

            // Pregatim listele pentru cazul in care intra un Admin 
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "Id", "FullName", appointment.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "Id", "FullName", appointment.PatientId);

            return View(appointment);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppointmentDate,DoctorId,PatientId,Status")] Appointment appointment)
        {
            if (id != appointment.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(appointment);
        }

        // GET: Appointments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();

            // Daca nu esti Admin si nici nu e programarea ta -> EROARE
            if (!User.IsInRole("Admin") &&
                !User.IsInRole("Doctor") &&
                appointment.Patient.Email != User.Identity.Name)
            {
                return Forbid(); // Sau return RedirectToAction("Index");
            }

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null) _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}