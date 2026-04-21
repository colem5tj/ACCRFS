using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Member
{
    public class ApproveHoursModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ApproveHoursModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public Transaction? Transaction { get; set; }
        public decimal ReceiverBalance { get; set; }
        public bool IsExempt { get; set; }

        public async Task<IActionResult> OnGetAsync(int transactionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            Transaction = await _db.Transactions
                .Include(t => t.Request)
                .Include(t => t.Provider)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId
                    && t.ReceiverId == userId);

            if (Transaction != null)
            {
                var receiver = await _db.Users.FindAsync(userId.Value);
                ReceiverBalance = receiver?.CurrentBalance ?? 0;
                IsExempt = HttpContext.Session.GetString("UserRole") == "OrganizationRep"
                           || (receiver?.IsSpecialAssistance ?? false);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int transactionId, int rating, string? comments, string startTime, string endTime)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            var transaction = await _db.Transactions
                .Include(t => t.Request)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId
                    && t.ReceiverId == userId);

            if (transaction == null)
                return NotFound();

            // Calculate actual hours from start/end times
            if (TimeSpan.TryParse(startTime, out var start) && TimeSpan.TryParse(endTime, out var end))
            {
                if (end <= start) end = end.Add(TimeSpan.FromHours(24)); // overnight
                var actualHours = Math.Round((decimal)(end - start).TotalHours, 2);

                var receiver = await _db.Users.FindAsync(transaction.ReceiverId);
                bool exempt = HttpContext.Session.GetString("UserRole") == "OrganizationRep"
                              || (receiver?.IsSpecialAssistance ?? false);
                if (!exempt)
                {
                    decimal maxAllowed = (receiver?.CurrentBalance ?? 0) + 3;
                    if (actualHours > maxAllowed)
                    {
                        TempData["Error"] = $"The entered duration ({actualHours} hrs) would bring your balance below -3. You can approve a maximum of {maxAllowed} hrs.";
                        return RedirectToPage(new { transactionId });
                    }
                }

                transaction.HoursTransferred = actualHours;
            }

            transaction.ConfirmedAt = DateTime.UtcNow;

            // Save feedback first so we have the rating
            var feedback = new Feedback
            {
                TransactionId = transaction.TransactionId,
                GivenByUserId = userId.Value,
                Rating = rating,
                Comments = comments
            };
            _db.FeedbackItems.Add(feedback);

            // Mark request as completed regardless of rating
            if (transaction.Request != null)
                transaction.Request.Status = "Completed";

            if (rating == 1)
            {
                // Hold hours pending admin review — balances not touched yet
                transaction.Status = "PendingReview";

                string volunteerName = (await _db.Users.FindAsync(transaction.ProviderId))?.FullName ?? "a volunteer";
                string requesterName = (await _db.Users.FindAsync(transaction.ReceiverId))?.FullName ?? "a member";
                string requestTitle  = transaction.Request?.Title ?? "a request";

                var adminIds = (from ur in _db.UserRoles
                                join r in _db.Roles on ur.RoleId equals r.RoleId
                                where r.RoleName == "Admin"
                                select ur.UserId).ToList();

                foreach (var adminId in adminIds)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId    = adminId,
                        Type      = "LowRating",
                        Message   = $"1-star rating: {requesterName} rated {volunteerName} 1/5 for \"{requestTitle}\". Hours are on hold pending your review.",
                        IsRead    = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                // Normal approval — credit provider, debit receiver, write ledger
                transaction.Status = "Approved";

                var provider = await _db.Users.FindAsync(transaction.ProviderId);
                if (provider != null)
                    provider.CurrentBalance += transaction.HoursTransferred;

                var receiver = await _db.Users.FindAsync(transaction.ReceiverId);
                if (receiver != null)
                    receiver.CurrentBalance -= transaction.HoursTransferred;

                _db.TimeLedgers.Add(new TimeLedger
                {
                    UserId           = transaction.ProviderId,
                    TransactionId    = transaction.TransactionId,
                    HoursChange      = transaction.HoursTransferred,
                    BalanceAfter     = provider?.CurrentBalance ?? 0
                });
                _db.TimeLedgers.Add(new TimeLedger
                {
                    UserId           = transaction.ReceiverId,
                    TransactionId    = transaction.TransactionId,
                    HoursChange      = -transaction.HoursTransferred,
                    BalanceAfter     = receiver?.CurrentBalance ?? 0
                });
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Hours approved! {transaction.HoursTransferred} hours have been awarded.";
            bool isOrg = HttpContext.Session.GetString("UserRole") == "OrganizationRep";
            return RedirectToPage(isOrg ? "/Organization/MyActivity" : "/Member/MyRequests");
        }
    }
}