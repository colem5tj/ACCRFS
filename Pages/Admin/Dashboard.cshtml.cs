using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ACC_Demo.Data;

namespace ACC_Demo.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int ActiveUsers { get; set; }
    public int OpenRequests { get; set; }
    public int PendingApprovals { get; set; }
    public int FlaggedUsers { get; set; }

    public List<UserVm> Users { get; set; } = new();
    public List<string> RecentReports { get; set; } = new();

    public IActionResult OnGet()
    {
        // Step 4: Guard – must be logged in to view this page
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToPage("/Account/Login");

        // Metrics
        ActiveUsers = _context.Users.Count(u => u.IsActive);
        OpenRequests = _context.Requests.Count(r => r.Status == "Open");
        PendingApprovals = _context.SkillVerifications.Count(v => !v.IsVerified);
        FlaggedUsers = _context.Users.Count(u => u.IsFlagged);

        // Users table
        Users = (from u in _context.Users
                 join ur in _context.UserRoles on u.UserId equals ur.UserId
                 join r in _context.Roles on ur.RoleId equals r.RoleId
                 select new UserVm
                 {
                     FullName = u.FullName,
                     Email = u.Email,
                     IsActive = u.IsActive,
                     RoleName = r.RoleName
                 }).Take(10).ToList();

        // Reports
        RecentReports = _context.AdminReports
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => r.Reason)
            .ToList();

        return Page();
    }

    public class UserVm
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
