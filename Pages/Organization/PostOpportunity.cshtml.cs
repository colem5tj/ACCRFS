using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Pages.Organization;

public class PostOpportunityModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public PostOpportunityModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty, Required, StringLength(150)]
    [Display(Name = "Request Title")]
    public string RequestTitle { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(2000)]
    [Display(Name = "Detailed Description")]
    public string DetailedDescription { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Primary Skill Needed")]
    public string? PrimarySkill { get; set; }

    [BindProperty, Required]
    [Display(Name = "Desired Start Date")]
    public DateTime DesiredStartDate { get; set; } = DateTime.Today.AddDays(7);

    [BindProperty, Required, Range(1, 100)]
    [Display(Name = "Estimated Hours")]
    public decimal EstimatedHours { get; set; } = 2;

    [BindProperty, Range(1, 500)]
    [Display(Name = "Volunteers Needed")]
    public int VolunteersNeeded { get; set; } = 1;

    [BindProperty]
    [Display(Name = "Mark as Urgent")]
    public bool MarkAsUrgent { get; set; }

    public IActionResult OnGet()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "OrganizationRep")
            return RedirectToPage("/Account/Login");

        return Page();
    }

    public IActionResult OnPost()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (userId == null || role != "OrganizationRep")
            return RedirectToPage("/Account/Login");

        var orgId = HttpContext.Session.GetInt32("OrganizationId");
        if (orgId == null)
        {
            orgId = _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.OrganizationId)
                .FirstOrDefault();

            if (orgId == null)
                return RedirectToPage("/Account/Login");

            HttpContext.Session.SetInt32("OrganizationId", orgId.Value);
        }

        if (!ModelState.IsValid)
            return Page();

        var request = new Request
        {
            CreatedByUserId = userId.Value,
            OrganizationId = orgId.Value,
            Title = RequestTitle,
            Description = DetailedDescription,
            HoursNeeded = EstimatedHours,
            UrgencyLevel = MarkAsUrgent ? "High" : "Medium",
            ScheduledDate = DesiredStartDate,
            VolunteersNeeded = VolunteersNeeded,
            Status = "Open"
        };

        _context.Requests.Add(request);
        _context.SaveChanges();

        return RedirectToPage("/Organization/Dashboard");
    }
}
