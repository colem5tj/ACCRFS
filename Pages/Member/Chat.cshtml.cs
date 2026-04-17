using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Pages.Member;

public class ChatModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public ChatModel(ApplicationDbContext context) => _context = context;

    [BindProperty(SupportsGet = true)]
    public int WithUserId { get; set; }

    public Models.User? OtherUser { get; set; }
    public List<Message> Messages { get; set; } = new();

    [BindProperty, Required, StringLength(1500, MinimumLength = 1)]
    public string NewMessage { get; set; } = string.Empty;

    public string ReportReason { get; set; } = string.Empty;
    public bool ReportSent { get; set; }

    private static readonly HashSet<string> BannedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "fuck", "shit", "ass", "asshole", "bitch", "cunt", "dick", "piss", "cock",
        "bastard", "damn", "crap", "whore", "slut", "nigger", "nigga", "faggot",
        "retard", "spic", "chink", "kike", "wetback", "tranny"
    };

    private static bool ContainsBannedWord(string text) =>
        BannedWords.Any(w =>
            System.Text.RegularExpressions.Regex.IsMatch(
                text,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(w)}\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Account/Login");
        int uid = userId.Value;

        if (WithUserId == 0 || WithUserId == uid)
            return RedirectToPage("/Member/Messages");

        OtherUser = _context.Users.FirstOrDefault(u => u.UserId == WithUserId
                                                     && u.IsActive && !u.IsBanned);
        if (OtherUser == null)
            return RedirectToPage("/Member/Messages");

        Messages = _context.Messages
            .Where(m => m.TransactionId == null
                     && ((m.SenderId == uid   && m.ReceiverId == WithUserId)
                      || (m.SenderId == WithUserId && m.ReceiverId == uid)))
            .Include(m => m.Sender)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        var unread = Messages.Where(m => m.ReceiverId == uid && !m.IsRead).ToList();
        foreach (var m in unread)
            m.IsRead = true;
        if (unread.Any())
            _context.SaveChanges();

        return Page();
    }

    public IActionResult OnPost()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Account/Login");
        int uid = userId.Value;

        if (!ModelState.IsValid)
            return OnGet();

        if (ContainsBannedWord(NewMessage))
        {
            ModelState.AddModelError(nameof(NewMessage),
                "Your message contains language that is not allowed. Please revise and try again.");
            return OnGet();
        }

        bool isBlocked = _context.UserBlocks.Any(b =>
            (b.BlockerUserId == WithUserId && b.BlockedUserId == uid) ||
            (b.BlockerUserId == uid        && b.BlockedUserId == WithUserId));
        if (isBlocked)
        {
            ModelState.AddModelError(string.Empty, "You cannot message this user.");
            return OnGet();
        }

        var message = new Message
        {
            SenderId            = uid,
            ReceiverId          = WithUserId,
            TransactionId       = null,
            Content             = NewMessage.Trim(),
            IsRead              = false,
            IsContactAnonymized = false,
            CreatedAt           = DateTime.UtcNow
        };
        _context.Messages.Add(message);

        var senderName = HttpContext.Session.GetString("UserName") ?? "A member";
        _context.Notifications.Add(new Notification
        {
            UserId    = WithUserId,
            Type      = "DirectMessage",
            Message   = $"{senderName} sent you a message.",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not send message: {ex.InnerException?.Message ?? ex.Message}");
            return OnGet();
        }

        return RedirectToPage(new { WithUserId });
    }

    public IActionResult OnPostReport()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Account/Login");
        int uid = userId.Value;

        ReportReason = Request.Form["ReportReason"].ToString().Trim();

        if (WithUserId == 0 || string.IsNullOrWhiteSpace(ReportReason))
        {
            ModelState.AddModelError(nameof(ReportReason), "Please provide a reason for the report.");
            return OnGet();
        }

        bool alreadyReported = _context.AdminReports.Any(r =>
            r.ReporterId == uid &&
            r.ReportedUserId == WithUserId &&
            r.Reason.StartsWith("[Chat Report]"));

        if (!alreadyReported)
        {
            _context.AdminReports.Add(new AdminReport
            {
                ReporterId     = uid,
                ReportedUserId = WithUserId,
                Reason         = $"[Chat Report] {ReportReason.Trim()}",
                CreatedAt      = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        ReportSent = true;
        return OnGet();
    }

    public IActionResult OnGetPoll(int withUserId, int after)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return new JsonResult(new { messages = Array.Empty<object>() });
        int uid = userId.Value;

        var newMessages = _context.Messages
            .Where(m => m.TransactionId == null
                     && m.MessageId > after
                     && ((m.SenderId == uid   && m.ReceiverId == withUserId)
                      || (m.SenderId == withUserId && m.ReceiverId == uid)))
            .Include(m => m.Sender)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        var unread = newMessages.Where(m => m.ReceiverId == uid && !m.IsRead).ToList();
        foreach (var m in unread)
            m.IsRead = true;
        if (unread.Any())
            _context.SaveChanges();

        var result = newMessages.Select(m => new
        {
            messageId = m.MessageId,
            content   = m.Content,
            isMe      = m.SenderId == uid,
            time      = m.CreatedAt.ToLocalTime().ToString("h:mm tt")
        });

        return new JsonResult(new { messages = result });
    }
}
