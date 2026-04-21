using ACC_Demo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Organization;

public class MyActivityModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MyActivityModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<RequestVm> Requests { get; set; } = new();

    public IActionResult OnGet()
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

        Requests = _context.Requests
            .Where(r => r.OrganizationId == orgId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RequestVm
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Description = r.Description,
                Status = r.Status,
                HoursNeeded = r.HoursNeeded,
                UrgencyLevel = r.UrgencyLevel,
                ScheduledDate = r.ScheduledDate,
                CreatedAt = r.CreatedAt,
                OfferCount = r.Transactions.Count(t => t.Status == "Pending"),
                AcceptedTransactionId = r.Transactions
                    .Where(t => t.Status == "Confirmed")
                    .Select(t => (int?)t.TransactionId)
                    .FirstOrDefault()
            })
            .ToList();

        return Page();
    }

    public class RequestVm
    {
        public int RequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal HoursNeeded { get; set; }
        public string UrgencyLevel { get; set; } = string.Empty;
        public DateTime? ScheduledDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OfferCount { get; set; }
        public int? AcceptedTransactionId { get; set; }
    }
}
