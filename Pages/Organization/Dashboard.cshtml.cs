using ACC_Demo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Organization;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string OrgName { get; set; } = string.Empty;
    public List<RequestCardVm> ActiveRequests { get; set; } = new();
    public List<IncomingOfferVm> IncomingOffers { get; set; } = new();
    public List<LedgerVm> RecentLedger { get; set; } = new();

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

        OrgName = HttpContext.Session.GetString("UserName") ?? string.Empty;

        ActiveRequests = _context.Requests
            .Where(r => r.OrganizationId == orgId && r.Status != "Completed" && r.Status != "Cancelled")
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new RequestCardVm
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Description = r.Description,
                Status = r.Status,
                HoursNeeded = r.HoursNeeded,
                UrgencyLevel = r.UrgencyLevel
            })
            .ToList();

        var orgRequestIds = _context.Requests
            .Where(r => r.OrganizationId == orgId)
            .Select(r => r.RequestId)
            .ToList();

        IncomingOffers = _context.Transactions
            .Include(t => t.Provider)
            .Include(t => t.Request)
            .Where(t => orgRequestIds.Contains(t.RequestId) && t.Status == "Pending")
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new IncomingOfferVm
            {
                TransactionId = t.TransactionId,
                RequestId = t.RequestId,
                ProviderName = t.Provider != null ? t.Provider.FullName : "Unknown",
                RequestTitle = t.Request != null ? t.Request.Title : "",
                Hours = t.HoursTransferred,
                OfferedAt = t.CreatedAt
            })
            .ToList();

        RecentLedger = _context.TimeLedgers
            .Include(l => l.Transaction)
            .ThenInclude(t => t!.Request)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new LedgerVm
            {
                Description = l.Transaction != null && l.Transaction.Request != null
                    ? l.Transaction.Request.Title
                    : "",
                HoursDelta = l.HoursChange > 0
                    ? $"+{l.HoursChange}"
                    : l.HoursChange.ToString()
            })
            .ToList();

        return Page();
    }

    public class RequestCardVm
    {
        public int RequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal HoursNeeded { get; set; }
        public string UrgencyLevel { get; set; } = string.Empty;
    }

    public class IncomingOfferVm
    {
        public int TransactionId { get; set; }
        public int RequestId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string RequestTitle { get; set; } = string.Empty;
        public decimal Hours { get; set; }
        public DateTime OfferedAt { get; set; }
    }

    public class LedgerVm
    {
        public string Description { get; set; } = string.Empty;
        public string HoursDelta { get; set; } = string.Empty;
    }
}
