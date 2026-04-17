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

        // Mark inbound unread messages as read
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

        _context.SaveChanges();
        return RedirectToPage(new { WithUserId });
    }

    // AJAX polling handler: returns new messages since the given message ID
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
