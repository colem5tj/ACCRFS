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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int transactionId, int rating)
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

            // Approve the transaction
            transaction.Status = "Approved";
            transaction.ConfirmedAt = DateTime.UtcNow;

            // Award hours to provider
            var provider = await _db.Users.FindAsync(transaction.ProviderId);
            if (provider != null)
                provider.CurrentBalance += transaction.HoursTransferred;

            // Save feedback rating
            var feedback = new Feedback
            {
                TransactionId = transaction.TransactionId,
                GivenByUserId = userId.Value,
                Rating = rating
            };
            _db.FeedbackItems.Add(feedback);

            // Mark request as completed
            if (transaction.Request != null)
                transaction.Request.Status = "Completed";

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Hours approved! {transaction.HoursTransferred} hours have been awarded.";
            return RedirectToPage("/Member/MyRequests");
        }
    }
}