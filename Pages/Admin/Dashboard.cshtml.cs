using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ACC_Demo.Data;

namespace ACC_Demo.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Metric cards ──────────────────────────────────────────────────────
    public int ActiveUsers { get; set; }
    public int OpenRequests { get; set; }
    public int PendingApprovals { get; set; }
    public int FlaggedUsers { get; set; }

    // ── Signed-in admin's own ID (used to hide self-targeting actions) ───
    public int CurrentUserId { get; set; }

    // ── Tab data ──────────────────────────────────────────────────────────
    public List<UserVm> Users { get; set; } = new();
    public List<UserVm> ArchivedUsers { get; set; } = new();
    public List<AdminReportVm> AdminReports { get; set; } = new();
    public List<UserVm> FlaggedUsersList { get; set; } = new();
    public List<RequestVm> AllRequests { get; set; } = new();
    public List<OfferVm> OfferActivity { get; set; } = new();
    public List<FeedbackVm> AllFeedback { get; set; } = new();
    public int LowRatingCount { get; set; }

    // ── Auth helper ───────────────────────────────────────────────────────
    private bool IsAdmin()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return false;
        return (from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.RoleId
                where ur.UserId == userId && r.RoleName == "Admin"
                select ur).Any();
    }

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToPage("/Account/Login");

        if (!IsAdmin())
            return RedirectToPage("/Index");

        CurrentUserId = userId.Value;

        // ── Metric cards ───────────────────────────────────────────────
        ActiveUsers = _context.Users.Count(u => u.IsActive);
        OpenRequests = _context.Requests.Count(r => r.Status == "Open");
        PendingApprovals = _context.SkillVerifications.Count(v => !v.IsVerified);
        FlaggedUsers = _context.Users.Count(u => u.IsFlagged);

        // ── User Management tab (excludes archived) ───────────────────
        Users = (from u in _context.Users
                 join ur in _context.UserRoles on u.UserId equals ur.UserId
                 join r in _context.Roles on ur.RoleId equals r.RoleId
                 where !u.IsArchived
                 orderby u.FullName
                 select new UserVm
                 {
                     UserId = u.UserId,
                     FullName = u.FullName,
                     Email = u.Email,
                     IsActive = u.IsActive,
                     IsBanned = u.IsBanned,
                     IsFlagged = u.IsFlagged,
                     IsSpecialAssistance = u.IsSpecialAssistance,
                     RoleName = r.RoleName
                 }).ToList();

        // ── Archive tab ────────────────────────────────────────────────
        ArchivedUsers = (from u in _context.Users
                         join ur in _context.UserRoles on u.UserId equals ur.UserId
                         join r in _context.Roles on ur.RoleId equals r.RoleId
                         where u.IsArchived
                         orderby u.FullName
                         select new UserVm
                         {
                             UserId = u.UserId,
                             FullName = u.FullName,
                             Email = u.Email,
                             IsActive = u.IsActive,
                             IsBanned = u.IsBanned,
                             IsFlagged = u.IsFlagged,
                             IsSpecialAssistance = u.IsSpecialAssistance,
                             IsArchived = true,
                             RoleName = r.RoleName
                         }).ToList();

        // ── Reports tab ────────────────────────────────────────────────
        AdminReports = (from ar in _context.AdminReports
                        join reporter in _context.Users on ar.ReporterId equals reporter.UserId
                        join reported in _context.Users on ar.ReportedUserId equals reported.UserId
                        orderby ar.CreatedAt descending
                        select new AdminReportVm
                        {
                            AdminReportId = ar.AdminReportId,
                            ReporterName = reporter.FullName,
                            ReporterEmail = reporter.Email,
                            ReportedUserName = reported.FullName,
                            ReportedUserEmail = reported.Email,
                            ReportedUserId = reported.UserId,
                            IsReportedUserFlagged = reported.IsFlagged,
                            Reason = ar.Reason,
                            CreatedAt = ar.CreatedAt
                        }).ToList();

        // ── Moderation tab ─────────────────────────────────────────────
        FlaggedUsersList = (from u in _context.Users
                            join ur in _context.UserRoles on u.UserId equals ur.UserId
                            join r in _context.Roles on ur.RoleId equals r.RoleId
                            where u.IsFlagged
                            orderby u.FullName
                            select new UserVm
                            {
                                UserId = u.UserId,
                                FullName = u.FullName,
                                Email = u.Email,
                                IsActive = u.IsActive,
                                IsBanned = u.IsBanned,
                                IsFlagged = u.IsFlagged,
                                IsSpecialAssistance = u.IsSpecialAssistance,
                                RoleName = r.RoleName
                            }).ToList();

        // ── All Requests tab ───────────────────────────────────────────
        AllRequests = (from req in _context.Requests
                       join u in _context.Users on req.CreatedByUserId equals u.UserId
                       orderby req.CreatedAt descending
                       select new RequestVm
                       {
                           RequestId = req.RequestId,
                           Title = req.Title,
                           RequesterName = u.FullName,
                           RequesterEmail = u.Email,
                           HoursNeeded = req.HoursNeeded,
                           Status = req.Status,
                           CreatedAt = req.CreatedAt,
                           AcceptedByName = req.AcceptedProviderId == null ? "—" :
                               _context.Users
                                   .Where(p => p.UserId == req.AcceptedProviderId)
                                   .Select(p => p.FullName)
                                   .FirstOrDefault() ?? "—"
                       }).ToList();

        // ── Offer Activity tab ─────────────────────────────────────────
        OfferActivity = (from req in _context.Requests
                         where req.AcceptedProviderId != null
                         join requester in _context.Users on req.CreatedByUserId equals requester.UserId
                         join provider in _context.Users on req.AcceptedProviderId equals provider.UserId
                         orderby req.CreatedAt descending
                         select new OfferVm
                         {
                             RequestId = req.RequestId,
                             RequestTitle = req.Title,
                             RequesterName = requester.FullName,
                             ProviderName = provider.FullName,
                             ProviderEmail = provider.Email,
                             HoursNeeded = req.HoursNeeded,
                             Status = req.Status,
                             OfferedAt = req.CreatedAt
                         }).ToList();

        // ── Feedback tab ───────────────────────────────────────────────
        AllFeedback = (from f in _context.FeedbackItems
                       join reviewer in _context.Users on f.GivenByUserId equals reviewer.UserId
                       join t in _context.Transactions on f.TransactionId equals t.TransactionId
                       join volunteer in _context.Users on t.ProviderId equals volunteer.UserId
                       select new FeedbackVm
                       {
                           FeedbackId        = f.FeedbackId,
                           TransactionId     = t.TransactionId,
                           TransactionStatus = t.Status,
                           GivenByName       = reviewer.FullName,
                           GivenByEmail      = reviewer.Email,
                           VolunteerName     = volunteer.FullName,
                           VolunteerEmail    = volunteer.Email,
                           Rating            = f.Rating ?? 0,
                           Comments          = f.Comments,
                           HoursTransferred  = t.HoursTransferred,
                           IsCheerful        = f.IsCheerful,
                           IsEfficient       = f.IsEfficient,
                           IsOnTime          = f.IsOnTime,
                           IsHighlySkilled   = f.IsHighlySkilled
                       }).ToList();

        // Badge only counts rows still awaiting admin action
        LowRatingCount = AllFeedback.Count(f => f.Rating == 1 && f.TransactionStatus == "PendingReview");

        return Page();
    }

    // ── POST: Archive user ────────────────────────────────────────────────
    public IActionResult OnPostArchiveUser(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsArchived = true;
            user.IsActive = false;
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    // ── POST: Unarchive user ──────────────────────────────────────────────
    public IActionResult OnPostUnarchiveUser(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsArchived = false;
            user.IsActive = true;
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    // ── POST: Delete user (only from archive; removes all dependent records) ─
    public IActionResult OnPostDeleteUser(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");

        var user = _context.Users.Find(userId);
        if (user == null || !user.IsArchived) return RedirectToPage();

        // Collect transaction IDs touching this user (as requester or provider)
        var requestIds = _context.Requests
            .Where(r => r.CreatedByUserId == userId)
            .Select(r => r.RequestId)
            .ToList();

        var txnIds = _context.Transactions
            .Where(t => t.ProviderId == userId || t.ReceiverId == userId
                        || requestIds.Contains(t.RequestId))
            .Select(t => t.TransactionId)
            .Distinct()
            .ToList();

        // Deepest children first
        _context.FeedbackItems.RemoveRange(
            _context.FeedbackItems.Where(f => txnIds.Contains(f.TransactionId)));

        _context.TimeLedgers.RemoveRange(
            _context.TimeLedgers.Where(l => txnIds.Contains(l.TransactionId) || l.UserId == userId));

        _context.Messages.RemoveRange(
            _context.Messages.Where(m => (m.TransactionId.HasValue && txnIds.Contains(m.TransactionId.Value))
                                         || m.SenderId == userId || m.ReceiverId == userId));

        _context.Transactions.RemoveRange(
            _context.Transactions.Where(t => txnIds.Contains(t.TransactionId)));

        // Null-out accepted provider on requests this user had accepted
        foreach (var req in _context.Requests.Where(r => r.AcceptedProviderId == userId))
            req.AcceptedProviderId = null;

        _context.Requests.RemoveRange(
            _context.Requests.Where(r => r.CreatedByUserId == userId));

        _context.AdminReports.RemoveRange(
            _context.AdminReports.Where(r => r.ReporterId == userId || r.ReportedUserId == userId));

        _context.UserBlocks.RemoveRange(
            _context.UserBlocks.Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId));

        _context.Notifications.RemoveRange(
            _context.Notifications.Where(n => n.UserId == userId));

        _context.UserLocationPreferences.RemoveRange(
            _context.UserLocationPreferences.Where(l => l.UserId == userId));

        _context.MediaItems.RemoveRange(
            _context.MediaItems.Where(m => m.UserId == userId));

        _context.Availabilities.RemoveRange(
            _context.Availabilities.Where(a => a.UserId == userId));

        _context.UserSkills.RemoveRange(
            _context.UserSkills.Where(us => us.UserId == userId));

        _context.OrganizationMembers.RemoveRange(
            _context.OrganizationMembers.Where(om => om.UserId == userId));

        _context.UserRoles.RemoveRange(
            _context.UserRoles.Where(ur => ur.UserId == userId));

        _context.Users.Remove(user);
        _context.SaveChanges();

        return RedirectToPage();
    }

    // ── POST: Ban user ────────────────────────────────────────────────────
    public IActionResult OnPostBanUser(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsBanned = true;
            user.IsActive = false;
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    // ── POST: Unban user ──────────────────────────────────────────────────
    public IActionResult OnPostUnbanUser(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsBanned = false;
            user.IsActive = true;
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    // ── POST: Flag user ───────────────────────────────────────────────────
    public IActionResult OnPostFlagUser(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsFlagged = true;
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    // ── POST: Unflag user ─────────────────────────────────────────────────
    public IActionResult OnPostUnflagUser(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsFlagged = false;
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    // ── POST: Grant special assistance ───────────────────────────────────
    public IActionResult OnPostGrantSpecialAssistance(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsSpecialAssistance = true;
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    // ── POST: Revoke special assistance ──────────────────────────────────
    public IActionResult OnPostRevokeSpecialAssistance(int userId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsSpecialAssistance = false;
            _context.SaveChanges();
        }
        return RedirectToPage();
    }

    // ── POST: Approve a held transaction (1-star review) ─────────────────
    public IActionResult OnPostApproveTransaction(int transactionId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");

        var transaction = _context.Transactions.Find(transactionId);
        if (transaction == null || transaction.Status != "PendingReview")
            return RedirectToPage();

        var provider = _context.Users.Find(transaction.ProviderId);
        if (provider != null)
            provider.CurrentBalance += transaction.HoursTransferred;

        var receiver = _context.Users.Find(transaction.ReceiverId);
        if (receiver != null)
            receiver.CurrentBalance -= transaction.HoursTransferred;

        _context.TimeLedgers.Add(new Models.TimeLedger
        {
            UserId        = transaction.ProviderId,
            TransactionId = transaction.TransactionId,
            HoursChange   = transaction.HoursTransferred,
            BalanceAfter  = provider?.CurrentBalance ?? 0
        });
        _context.TimeLedgers.Add(new Models.TimeLedger
        {
            UserId        = transaction.ReceiverId,
            TransactionId = transaction.TransactionId,
            HoursChange   = -transaction.HoursTransferred,
            BalanceAfter  = receiver?.CurrentBalance ?? 0
        });

        transaction.Status = "Approved";
        _context.SaveChanges();

        return RedirectToPage();
    }

    // ── POST: Reject a held transaction (1-star review) ──────────────────
    public IActionResult OnPostRejectTransaction(int transactionId)
    {
        if (!IsAdmin()) return RedirectToPage("/Account/Login");

        var transaction = _context.Transactions.Find(transactionId);
        if (transaction == null || transaction.Status != "PendingReview")
            return RedirectToPage();

        transaction.Status = "Rejected";
        _context.SaveChanges();

        return RedirectToPage();
    }

    // ── View models ────────────────────────────────────────────────────────
    public class UserVm
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public bool IsFlagged { get; set; }
        public bool IsSpecialAssistance { get; set; }
        public bool IsArchived { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class AdminReportVm
    {
        public int AdminReportId { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterEmail { get; set; } = string.Empty;
        public string ReportedUserName { get; set; } = string.Empty;
        public string ReportedUserEmail { get; set; } = string.Empty;
        public int ReportedUserId { get; set; }
        public bool IsReportedUserFlagged { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RequestVm
    {
        public int RequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public decimal HoursNeeded { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AcceptedByName { get; set; } = "—";
    }

    public class OfferVm
    {
        public int RequestId { get; set; }
        public string RequestTitle { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderEmail { get; set; } = string.Empty;
        public decimal HoursNeeded { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OfferedAt { get; set; }
    }

    public class FeedbackVm
    {
        public int FeedbackId { get; set; }
        public int TransactionId { get; set; }
        public string TransactionStatus { get; set; } = string.Empty;
        public string GivenByName { get; set; } = string.Empty;
        public string GivenByEmail { get; set; } = string.Empty;
        public string VolunteerName { get; set; } = string.Empty;
        public string VolunteerEmail { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comments { get; set; }
        public decimal HoursTransferred { get; set; }
        public bool IsCheerful { get; set; }
        public bool IsEfficient { get; set; }
        public bool IsOnTime { get; set; }
        public bool IsHighlySkilled { get; set; }
    }
}
