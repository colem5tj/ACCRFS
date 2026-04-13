using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ACC_Demo.Data;
using ACC_Demo.Models;

namespace ACC_Demo.Pages.Member;

public class RequestDetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public RequestDetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Request? RequestItem { get; set; }

    public double? CreatorLat { get; set; }
    public double? CreatorLng { get; set; }

    public IActionResult OnGet(int id)
    {
        RequestItem = _context.Requests.FirstOrDefault(r => r.RequestId == id);

        if (RequestItem == null)
            return RedirectToPage("/Member/BrowseRequests");

        var locPref = _context.UserLocationPreferences
            .FirstOrDefault(l => l.UserId == RequestItem.CreatedByUserId
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

    public IActionResult OnPostAccept()
    {
        const int demoProviderId = 1;

        var request = _context.Requests.FirstOrDefault(r => r.RequestId == RequestItem!.RequestId);
        if (request == null)
            return RedirectToPage("/Member/BrowseRequests");

        request.Status = "Matched";

        _context.Transactions.Add(new Transaction
        {
            RequestId        = request.RequestId,
            ProviderId       = demoProviderId,
            ReceiverId       = request.CreatedByUserId,
            OrganizationId   = request.OrganizationId,
            HoursTransferred = request.HoursNeeded,
            Status           = "Pending"
        });

        _context.SaveChanges();

        return RedirectToPage("/Member/MyRequests");
    }

    public IActionResult OnPostDecline()
    {
        return RedirectToPage("/Member/BrowseRequests");
    }
}
