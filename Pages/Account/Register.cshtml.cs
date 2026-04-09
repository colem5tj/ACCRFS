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
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            FullName = $"{Input.FirstName} {Input.LastName}",
            Email = Input.Email,
            PhoneNumber = Input.PhoneNumber,
            Occupation = Input.Occupation,
            OrganizationName = Input.OrganizationName,
            Bio = Input.Bio,
            ParticipationPlan = Input.ParticipationPlan,
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

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserName", user.FullName);
        HttpContext.Session.SetString("UserRole", "Member");

        return RedirectToPage("/Member/Dashboard");
    }

    public class RegisterInputModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? Occupation { get; set; }

        [Display(Name = "Organization Name")]
        public string? OrganizationName { get; set; }

        public string? Bio { get; set; }

        [Display(Name = "How do you plan on participating?")]
        public string? ParticipationPlan { get; set; }

        [Display(Name = "Skill Summary")]
        public string? SkillSummary { get; set; }

        [Display(Name = "Search Radius (Miles)")]
        public decimal SearchRadiusMiles { get; set; } = 25;

        [Display(Name = "Hide My Location")]
        public bool IsLocationHidden { get; set; } = true;

        [Display(Name = "Preferred Contact Method")]
        public string PreferredContactMethod { get; set; } = "Email";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}