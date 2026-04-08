using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ACC_Demo.Data;

namespace ACC_Demo.Pages.Member;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public decimal CurrentBalance { get; set; }
    public List<RequestCardVm> ActiveRequests { get; set; } = new();
    public List<TransactionVm> AcceptedTransactions { get; set; } = new();
    public List<LedgerVm> RecentLedger { get; set; } = new();

    public IActionResult OnGet()
    {
        // Step 4: Read the logged-in user from session instead of a hardcoded ID
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToPage("/Account/Login");

        int demoUserId = userId.Value;

        CurrentBalance = _context.Users
            .Where(u => u.UserId == demoUserId)
            .Select(u => u.CurrentBalance)
            .FirstOrDefault();

        ActiveRequests = _context.Requests
            .Where(r => r.CreatedByUserId == demoUserId)
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

        AcceptedTransactions = _context.Transactions
            .Include(t => t.Request)
            .Where(t => t.ProviderId == demoUserId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new TransactionVm
            {
                TransactionId = t.TransactionId,
                RequestTitle = t.Request != null ? t.Request.Title : "",
                Hours = t.HoursTransferred
            })
            .ToList();

        RecentLedger = _context.TimeLedgers
            .Include(l => l.Transaction)
            .ThenInclude(t => t!.Request)
            .Where(l => l.UserId == demoUserId)
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

    public class TransactionVm
    {
        public int TransactionId { get; set; }
        public string RequestTitle { get; set; } = string.Empty;
        public decimal Hours { get; set; }
    }

    public class LedgerVm
    {
        public string Description { get; set; } = string.Empty;
        public string HoursDelta { get; set; } = string.Empty;
    }
}
