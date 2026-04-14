using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Member
{
    public class OfferHelpModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public OfferHelpModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public Request? Request { get; set; }
        public double? CreatorLat { get; set; }
        public double? CreatorLng { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            Request = await _db.Requests
                .Include(r => r.CreatedByUser)
                .Include(r => r.Skill)
                .Include(r => r.Organization)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (Request == null)
                return NotFound();

            if (Request.CreatedByUserId == userId)
                return RedirectToPage("/Member/BrowseRequests");

            var locPref = _db.UserLocationPreferences
                .FirstOrDefault(l => l.UserId == Request.CreatedByUserId
                                   && !l.IsLocationHidden
                                   && l.ApproxLatitude != null
                                   && l.ApproxLongitude != null);

            if (locPref != null)
            {
                CreatorLat = (double)locPref.ApproxLatitude!.Value;
                CreatorLng = (double)locPref.ApproxLongitude!.Value;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int requestId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            var request = await _db.Requests.FindAsync(requestId);
            if (request == null)
                return NotFound();

            // Check if this user already offered help on this request
            bool alreadyOffered = await _db.Transactions.AnyAsync(t =>
                t.RequestId == requestId && t.ProviderId == userId);

            if (!alreadyOffered)
            {
                var transaction = new Transaction
                {
                    RequestId = requestId,
                    ProviderId = userId.Value,
                    ReceiverId = request.CreatedByUserId,
                    HoursTransferred = request.HoursNeeded,
                    Status = "Pending"
                };

                _db.Transactions.Add(transaction);
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Your offer has been sent!";
            return RedirectToPage("/Member/BrowseRequests");
        }
    }
}