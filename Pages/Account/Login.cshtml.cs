using System.ComponentModel.DataAnnotations;
using ACC_Demo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ACC_Demo.Pages.Account;

public class LoginModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public LoginModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = _context.Users
            .FirstOrDefault(u => u.Email == Input.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash))
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        if (user.IsBanned)
        {
            ErrorMessage = "Your account has been suspended. Please contact support.";
            return Page();
        }

        var roleId = _context.UserRoles
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.RoleId)
            .FirstOrDefault();

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserName", user.FullName);
        HttpContext.Session.SetString("UserRole", roleId == 1 ? "Admin" : roleId == 3 ? "OrganizationRep" : "Member");

        return RedirectToDashboardFor(roleId);
    }

    public IActionResult OnPostDemoLogin(int userId)
    {
        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
        if (user == null) return RedirectToPage();

        var roleId = _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .FirstOrDefault();

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserName", user.FullName);
        HttpContext.Session.SetString("UserRole", roleId == 1 ? "Admin" : roleId == 3 ? "OrganizationRep" : "Member");

        return RedirectToDashboardFor(roleId);
    }

    private IActionResult RedirectToDashboardFor(int roleId)
    {
        return roleId switch
        {
            1 => RedirectToPage("/Admin/Dashboard"),
            3 => RedirectToPage("/Organization/Dashboard"),
            _ => RedirectToPage("/Member/Dashboard")
        };
    }

    public class LoginInputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}