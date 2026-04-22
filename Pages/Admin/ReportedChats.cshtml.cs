using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Admin;

public class ReportedChatsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public ReportedChatsModel(ApplicationDbContext context) => _context = context;

    public List<ReportedChatVm> Reports { get; set; } = new();

    private bool IsAdmin()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return false;
        return (from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.RoleId
                where ur.UserId == userId && r.RoleName == "Admin"
                select ur).Any();
    }

    public IActionResult OnPostDismiss(int reportId)
    {
        if (!IsAdmin()) return RedirectToPage("/Index");
        var report = _context.AdminReports.Find(reportId);
        if (report != null)
        {
            _context.AdminReports.Remove(report);
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    public IActionResult OnGet()
    {
        if (!IsAdmin()) return RedirectToPage("/Index");

        var chatReports = _context.AdminReports
            .Where(r => r.Reason.StartsWith("[Chat Report]"))
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        Reports = chatReports.Select(r =>
        {
            var messages = _context.Messages
                .Where(m => m.TransactionId == null
                         && ((m.SenderId == r.ReporterId   && m.ReceiverId == r.ReportedUserId)
                          || (m.SenderId == r.ReportedUserId && m.ReceiverId == r.ReporterId)))
                .Include(m => m.Sender)
                .OrderBy(m => m.CreatedAt)
                .ToList();

            return new ReportedChatVm
            {
                AdminReportId  = r.AdminReportId,
                ReporterName   = r.Reporter?.FullName ?? "Unknown",
                ReportedName   = r.ReportedUser?.FullName ?? "Unknown",
                Reason         = r.Reason.Replace("[Chat Report] ", string.Empty),
                ReportedAt     = r.CreatedAt,
                Messages       = messages
            };
        }).ToList();

        return Page();
    }

    public class ReportedChatVm
    {
        public int AdminReportId    { get; set; }
        public string ReporterName  { get; set; } = string.Empty;
        public string ReportedName  { get; set; } = string.Empty;
        public string Reason        { get; set; } = string.Empty;
        public DateTime ReportedAt  { get; set; }
        public List<Message> Messages { get; set; } = new();
    }
}
