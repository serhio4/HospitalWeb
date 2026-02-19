using HospitalWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HospitalWeb.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            var userManager = service.GetService<UserManager<IdentityUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();
            var context = service.GetService<ApplicationDbContext>();

            // 1. ASIGURARE ROLURI
            await EnsureRoleAsync(roleManager, "Admin");
            await EnsureRoleAsync(roleManager, "Doctor");
            await EnsureRoleAsync(roleManager, "Patient");

            // 2. CREARE ADMIN
            var adminEmail = "admin@hospital.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                await userManager.CreateAsync(adminUser, "Parola123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // ---------------------------------------------------------
            // 3. POPULARE DEPARTAMENTE (Daca nu exista)
            // ---------------------------------------------------------
            if (!context.Departments.Any())
            {
                var departments = new List<Department>
                {
                    new Department { Name = "Cardiologie" },
                    new Department { Name = "Neurologie" },
                    new Department { Name = "Ortopedie" },
                    new Department { Name = "Pediatrie" },
                    new Department { Name = "Dermatologie" },
                    new Department { Name = "Oftalmologie" },
                    new Department { Name = "Chirurgie Generala" },
                    new Department { Name = "Gastroenterologie" },
                    new Department { Name = "Endocrinologie" },
                    new Department { Name = "Psihiatrie" }
                };
                context.Departments.AddRange(departments);
                await context.SaveChangesAsync();
            }

            // ---------------------------------------------------------
            // 4. GENERARE DOCTORI (Si conturi de Login pt ei)
            // ---------------------------------------------------------
            if (!context.Doctors.Any())
            {
                var departments = context.Departments.ToList();
                var doctorNames = new[] { "Popescu", "Ionescu", "Georgescu", "Dumitrescu", "Stan", "Stancu", "Gheorghe", "Marin", "Tudor", "Dobre" };
                var doctorFirstNames = new[] { "Andrei", "Maria", "Elena", "Ionut", "Alexandru", "Ioana", "Mihai", "Cristina", "Sorin", "Diana" };

                var random = new Random();

                foreach (var dept in departments)
                {
                    // Cream cate 2 doctori pentru fiecare departament
                    for (int i = 0; i < 2; i++)
                    {
                        var lastName = doctorNames[random.Next(doctorNames.Length)];
                        var firstName = doctorFirstNames[random.Next(doctorFirstNames.Length)];
                        var fullName = $"{firstName} {lastName}";
                        var email = $"{firstName.ToLower()}.{lastName.ToLower()}{random.Next(100, 999)}@hospital.com";

                        // A. Cream Userul de Login
                        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                        var result = await userManager.CreateAsync(user, "Parola123!");

                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "Doctor");

                            // B. Cream Profilul de Doctor
                            var doctor = new Doctor
                            {
                                FullName = "Dr. " + fullName,
                                Email = email,
                                DepartmentId = dept.Id
                            };
                            context.Doctors.Add(doctor);
                        }
                    }
                }
                await context.SaveChangesAsync();
            }

            // ---------------------------------------------------------
            // 5. GENERARE PACIENTI (Si conturi de Login pt ei)
            // ---------------------------------------------------------
            if (!context.Patients.Any())
            {
                var firstNames = new[] { "Ana", "Dan", "Vlad", "Carmen", "Adrian", "Simona", "Victor", "Irina", "George", "Laura" };
                var lastNames = new[] { "Munteanu", "Popa", "Rusu", "Rotaru", "Nistor", "Sandu", "Moldovan", "Diaconu", "Filip", "Lazarescu" };
                var random = new Random();

                for (int i = 0; i < 30; i++)
                {
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    var firstName = firstNames[random.Next(firstNames.Length)];
                    var fullName = $"{firstName} {lastName}";
                    var email = $"pacient{i + 1}@test.com"; // Email simplu pt testare: pacient1@test.com, pacient2...

                    // A. Cream Userul
                    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, "Parola123!");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Patient");

                        // B. Cream Profilul
                        var patient = new Patient
                        {
                            FullName = fullName,
                            Email = email,
                            PhoneNumber = "07" + random.Next(10000000, 99999999),
                            CNP = "1" + random.Next(100000, 999999) + random.Next(100000, 999999)
                        };
                        context.Patients.Add(patient);
                    }
                }
                await context.SaveChangesAsync();
            }

            // ---------------------------------------------------------
            // 6. GENERARE PROGRAMARI SI REVIEW-URI
            // ---------------------------------------------------------
            if (!context.Appointments.Any())
            {
                var doctors = context.Doctors.ToList();
                var patients = context.Patients.ToList();
                var random = new Random();
                var appointmentsList = new List<Appointment>();

                // A. Programari in TRECUT (Finalizate + Review-uri)
                for (int i = 0; i < 50; i++)
                {
                    var doc = doctors[random.Next(doctors.Count)];
                    var pat = patients[random.Next(patients.Count)];
                    var daysBack = random.Next(1, 60); // Intre 1 si 60 de zile in urma

                    var app = new Appointment
                    {
                        DoctorId = doc.Id,
                        PatientId = pat.Id,
                        AppointmentDate = DateTime.Now.AddDays(-daysBack).AddHours(random.Next(9, 17)),
                        Status = AppointmentStatus.Completed
                    };

                    // Adaugam un Review aleatoriu la unele dintre ele (80% sansa)
                    if (random.NextDouble() > 0.2)
                    {
                        app.Review = new Review
                        {
                            Rating = random.Next(1, 6), // Nota 1-5
                            Comment = GenerateRandomComment(random.Next(1, 6))
                        };
                    }
                    appointmentsList.Add(app);
                }

                // B. Programari in VIITOR (Pending sau Confirmed)
                for (int i = 0; i < 40; i++)
                {
                    var doc = doctors[random.Next(doctors.Count)];
                    var pat = patients[random.Next(patients.Count)];
                    var daysForward = random.Next(1, 30);

                    var status = (random.NextDouble() > 0.5) ? AppointmentStatus.Confirmed : AppointmentStatus.Pending;

                    appointmentsList.Add(new Appointment
                    {
                        DoctorId = doc.Id,
                        PatientId = pat.Id,
                        AppointmentDate = DateTime.Now.AddDays(daysForward).AddHours(random.Next(9, 17)),
                        Status = status
                    });
                }

                context.Appointments.AddRange(appointmentsList);
                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private static string GenerateRandomComment(int rating)
        {
            var goodComments = new[] { "Excelent medic!", "Foarte multumit.", "Recomand cu incredere.", "A fost foarte atent.", "Profesionalism desavarsit." };
            var badComments = new[] { "Nu mi-a placut.", "A intarziat.", "Prea grabit.", "Nu recomand.", "Se putea mai bine." };
            var avgComments = new[] { "A fost ok.", "Destul de bine.", "Acceptabil.", "Medic bun, dar se asteapta mult.", "Multumit." };

            var random = new Random();
            if (rating >= 4) return goodComments[random.Next(goodComments.Length)];
            if (rating <= 2) return badComments[random.Next(badComments.Length)];
            return avgComments[random.Next(avgComments.Length)];
        }
    }
}