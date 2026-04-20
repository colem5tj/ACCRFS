using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.IsMinor)
        {
            if (string.IsNullOrWhiteSpace(Input.ParentEmail))
            {
                ModelState.AddModelError("Input.ParentEmail", "A parent/guardian email is required.");
                return Page();
            }

            var parent = await _context.Users.FirstOrDefaultAsync(u => u.Email == Input.ParentEmail);

            if (parent == null)
            {
                ModelState.AddModelError("Input.ParentEmail",
                    "No adult account was found with that email. Please ask your parent/guardian to register first.");
                return Page();
            }

            if (parent.IsMinor)
            {
                ModelState.AddModelError("Input.ParentEmail",
                    "That email belongs to a minor account and cannot be used as a guardian.");
                return Page();
            }
        }

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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
            CurrentBalance = 0,
            IsMinor = Input.IsMinor
        };

        if (Input.IsMinor && !string.IsNullOrWhiteSpace(Input.ParentEmail))
        {
            var parent = await _context.Users.FirstOrDefaultAsync(u => u.Email == Input.ParentEmail);
            user.ParentUserId = parent!.UserId;
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = 2 });
        _context.UserLocationPreferences.Add(new UserLocationPreference
        {
            UserId = user.UserId,
            ZipCode = Input.ZipCode,
            SearchRadiusMiles = Input.SearchRadiusMiles,
            IsLocationHidden = !Input.IsLocationHidden
        });
        await _context.SaveChangesAsync();

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

        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Enter a valid ZIP code (e.g. 12345 or 12345-6789).")]
        [Display(Name = "ZIP Code")]
        public string? ZipCode { get; set; }

        [Display(Name = "Search Radius (Miles)")]
        public decimal SearchRadiusMiles { get; set; } = 25;

        [Display(Name = "Show General Area on Requests")]
        public bool IsLocationHidden { get; set; } = false;

        [Display(Name = "Preferred Contact Method")]
        public string PreferredContactMethod { get; set; } = "Email";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool IsMinor { get; set; } = false;

        [EmailAddress]
        [Display(Name = "Parent/Guardian Email")]
        public string? ParentEmail { get; set; }
    }
}