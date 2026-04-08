using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public RegisterModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = new User
        {
            FullName = Input.FullName,
            Email = Input.Email,
            PhoneNumber = Input.PhoneNumber,
            Occupation = Input.Occupation,
            Bio = Input.Bio,
            PasswordHash = Input.Password,
            CurrentBalance = 0
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        _context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = 2 });
        _context.UserLocationPreferences.Add(new UserLocationPreference
        {
            UserId = user.UserId,
            SearchRadiusMiles = Input.SearchRadiusMiles,
            IsLocationHidden = Input.IsLocationHidden
        });
        _context.SaveChanges();

        // Set session so user is logged in immediately after registration
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserName", user.FullName);
        HttpContext.Session.SetString("UserRole", "Member");

        return RedirectToPage("/Member/Dashboard");
    }

    public class RegisterInputModel
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? Occupation { get; set; }
        public string? Bio { get; set; }
        public string? SkillSummary { get; set; }

        public decimal SearchRadiusMiles { get; set; } = 25;
        public bool IsLocationHidden { get; set; } = true;
        public string PreferredContactMethod { get; set; } = "Email";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}