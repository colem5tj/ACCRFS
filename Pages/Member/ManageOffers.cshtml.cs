using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Member
{
    public class ManageOffersModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ManageOffersModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public Request? Request { get; set; }
        public List<Transaction> Offers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int requestId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            Request = await _db.Requests
                .Include(r => r.AcceptedProvider)
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.CreatedByUserId == userId);

            if (Request == null)
                return RedirectToPage("/Member/MyRequests");

            Offers = await _db.Transactions
                .Include(t => t.Provider)
                .Where(t => t.RequestId == requestId)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int transactionId, int requestId, int providerId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            var request = await _db.Requests.FindAsync(requestId);
            var transaction = await _db.Transactions.FindAsync(transactionId);

            if (request == null || transaction == null)
                return NotFound();

            // Accept the offer
            request.AcceptedProviderId = providerId;
            request.Status = "Matched";
            transaction.Status = "Confirmed";

            await _db.SaveChangesAsync();

            TempData["Success"] = "Offer accepted! The member has been matched to your request.";
            return RedirectToPage("/Member/ManageOffers", new { requestId });
        }
    }
}