using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Member
{
    public class LedgerModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LedgerModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int CurrentUserId { get; set; }
        public decimal Balance { get; set; }
        public decimal PendingHours { get; set; }
        public decimal AvgPerWeek { get; set; }
        public string LastUpdateLabel { get; set; } = "N/A";
        public List<LedgerRowVm> Rows { get; set; } = new();

        public async Task OnGetAsync()
        {
            CurrentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

            // Use the stored balance (includes admin credits)
            var user = await _context.Users.FindAsync(CurrentUserId);
            Balance = user?.CurrentBalance ?? 0;

            var transactions = await _context.Transactions
                .Include(t => t.Request)
                .Where(t => t.ProviderId == CurrentUserId || t.ReceiverId == CurrentUserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var adminCredits = await _context.TimeLedgers
                .Where(l => l.UserId == CurrentUserId && l.TransactionId == null)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var rows = new List<LedgerRowVm>();

            foreach (var tx in transactions)
            {
                bool isProvider = tx.ProviderId == CurrentUserId;
                rows.Add(new LedgerRowVm
                {
                    Date        = tx.CreatedAt,
                    Description = tx.Request?.Title ?? "Transaction #" + tx.TransactionId,
                    HoursChange = isProvider ? tx.HoursTransferred : -tx.HoursTransferred,
                    Status      = tx.Status,
                    EntryId     = "TX-" + tx.TransactionId,
                    IsIncoming  = isProvider
                });
            }

            foreach (var credit in adminCredits)
            {
                rows.Add(new LedgerRowVm
                {
                    Date        = credit.CreatedAt,
                    Description = credit.Description ?? "Admin Credit",
                    HoursChange = credit.HoursChange,
                    Status      = "AdminCredit",
                    EntryId     = "Credit",
                    IsIncoming  = true
                });
            }

            Rows = rows.OrderByDescending(r => r.Date).ToList();

            PendingHours = transactions
                .Where(t => t.ProviderId == CurrentUserId && t.Status == "Pending")
                .Sum(t => t.HoursTransferred);

            var approvedEarned = transactions
                .Where(t => t.ProviderId == CurrentUserId && t.Status == "Approved")
                .Sum(t => t.HoursTransferred);

            var oldest = transactions.LastOrDefault();
            if (oldest != null)
            {
                double weeks = Math.Max(1, (DateTime.UtcNow - oldest.CreatedAt).TotalDays / 7);
                AvgPerWeek = Math.Round(approvedEarned / (decimal)weeks, 1);
            }

            var latest = Rows.FirstOrDefault();
            if (latest != null)
            {
                var diff = DateTime.UtcNow - latest.Date;
                LastUpdateLabel = diff.TotalHours < 1 ? "JUST NOW"
                    : diff.TotalHours < 24 ? $"{(int)diff.TotalHours}H AGO"
                    : $"{(int)diff.TotalDays}D AGO";
            }
        }

        public class LedgerRowVm
        {
            public DateTime Date { get; set; }
            public string Description { get; set; } = string.Empty;
            public decimal HoursChange { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? EntryId { get; set; }
            public bool IsIncoming { get; set; }
        }
    }
}
