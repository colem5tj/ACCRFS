using ACC_Demo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Organization;

public class MessagesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public MessagesModel(ApplicationDbContext context) => _context = context;

    public List<ConversationVm> Conversations { get; set; } = new();
    public int TotalUnread { get; set; }

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (userId == null || role != "OrganizationRep")
            return RedirectToPage("/Account/Login");

        int uid = userId.Value;

        var allDms = _context.Messages
            .Where(m => m.TransactionId == null
                     && (m.SenderId == uid || m.ReceiverId == uid))
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderByDescending(m => m.CreatedAt)
            .ToList();

        Conversations = allDms
            .GroupBy(m => m.SenderId == uid ? m.ReceiverId : m.SenderId)
            .Select(g =>
            {
                var latest = g.First();
                var partner = latest.SenderId == uid ? latest.Receiver : latest.Sender;
                return new ConversationVm
                {
                    OtherUserId   = g.Key,
                    OtherUserName = partner?.FullName ?? "Unknown",
                    LatestMessage = latest.Content.Length > 80
                                    ? latest.Content[..80] + "\u2026"
                                    : latest.Content,
                    LatestAt      = latest.CreatedAt,
                    UnreadCount   = g.Count(m => m.ReceiverId == uid && !m.IsRead)
                };
            })
            .OrderByDescending(c => c.LatestAt)
            .ToList();

        TotalUnread = Conversations.Sum(c => c.UnreadCount);
        return Page();
    }

    public class ConversationVm
    {
        public int OtherUserId      { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string LatestMessage { get; set; } = string.Empty;
        public DateTime LatestAt    { get; set; }
        public int UnreadCount      { get; set; }
    }
}
