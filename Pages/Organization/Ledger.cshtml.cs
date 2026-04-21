using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Organization;

public class LedgerModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public LedgerModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int CurrentUserId { get; set; }
    public decimal Balance { get; set; }
    public decimal AllocatedHours { get; set; }
    public decimal PendingHours { get; set; }
    public decimal AvgPerWeek { get; set; }
    public string AllocatedLabel { get; set; } = "";
    public string LastUpdateLabel { get; set; } = "N/A";
    public List<Transaction> Transactions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (userId == null || role != "OrganizationRep")
            return RedirectToPage("/Account/Login");

        CurrentUserId = userId.Value;

        Transactions = await _context.Transactions
            .Include(t => t.Request)
            .Where(t => t.ProviderId == CurrentUserId || t.ReceiverId == CurrentUserId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var approved = Transactions.Where(t => t.Status == "Approved");
        decimal earned = approved.Where(t => t.ProviderId == CurrentUserId).Sum(t => t.HoursTransferred);
        decimal spent  = approved.Where(t => t.ReceiverId == CurrentUserId).Sum(t => t.HoursTransferred);
        Balance = earned - spent;

        PendingHours = Transactions
            .Where(t => t.ProviderId == CurrentUserId && t.Status == "Pending")
            .Sum(t => t.HoursTransferred);

        var allocated = Transactions
            .Where(t => t.ReceiverId == CurrentUserId && t.Status != "Approved")
            .FirstOrDefault();
        AllocatedHours = allocated?.HoursTransferred ?? 0;
        AllocatedLabel = allocated?.Request?.Title ?? "";

        var first = Transactions.LastOrDefault();
        if (first != null)
        {
            double weeks = Math.Max(1, (DateTime.UtcNow - first.CreatedAt).TotalDays / 7);
            AvgPerWeek = Math.Round(earned / (decimal)weeks, 1);
        }

        var latest = Transactions.FirstOrDefault();
        if (latest != null)
        {
            var diff = DateTime.UtcNow - latest.CreatedAt;
            LastUpdateLabel = diff.TotalHours < 1 ? "JUST NOW"
                : diff.TotalHours < 24 ? $"{(int)diff.TotalHours}H AGO"
                : $"{(int)diff.TotalDays}D AGO";
        }

        return Page();
    }
}
