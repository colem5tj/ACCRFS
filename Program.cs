using Microsoft.AspNetCore.Localization;
using System.Globalization;
using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.EntityFrameworkCore;
using ACC_Demo;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResources));
    });
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddLocalization();

var app = builder.Build();

// ?? Database migration + admin seed ???????????????????????????????????????
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Seed admin account if it doesn't exist
    const string adminEmail = "alachuacommunitycollective@gmail.com";
    if (!db.Users.Any(u => u.Email == adminEmail))
    {
        // 1. Create the admin user
        var adminUser = new User
        {
            FullName = "Site Administrator",
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            IsActive = true,
            IsFlagged = false,
            IsBanned = false,
            CreatedAt = DateTime.UtcNow,
            CurrentBalance = 0
        };
        db.Users.Add(adminUser);
        db.SaveChanges();

        // 2. Assign the Admin role (RoleId = 1 per your seed data)
        var adminRole = new UserRole
        {
            UserId = adminUser.UserId,
            RoleId = 1
        };
        db.UserRoles.Add(adminRole);
        db.SaveChanges();
    }

    // Re-hash any passwords that were stored as plain text (no BCrypt prefix)
    var plainTextUsers = db.Users
        .Where(u => !u.PasswordHash.StartsWith("$2"))
        .ToList();

    if (plainTextUsers.Any())
    {
        foreach (var u in plainTextUsers)
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.PasswordHash);

        db.SaveChanges();
    }
}

// ?? Localization ???????????????????????????????????????????????????????????
var supportedCultures = new[] { "en", "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
localizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
localizationOptions.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization(localizationOptions);
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();
app.Run();