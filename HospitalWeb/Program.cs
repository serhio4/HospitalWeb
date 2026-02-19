using HospitalWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurare conexiune la Baza de Date 
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurare Identity (Cu Roluri activate)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // <--- Activare roluri
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 3. Adaugare servicii MVC (Controllere si View-uri)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Executare Seeder pentru Roluri si Admin 
// Ruleaza doar daca baza de date e goala de roluri/admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Apelam metoda statica din DbSeeder
        await HospitalWeb.Data.DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        // Putem loga eroarea daca apare ceva la creare
        Console.WriteLine("A aparut o eroare la crearea rolurilor: " + ex.Message);
    }
}

// 5. Configurare Pipeline (HTTP Request)
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Intai Autentificare, apoi Autorizare
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // Necesar pentru paginile de Login/Register din Identity

app.Run();