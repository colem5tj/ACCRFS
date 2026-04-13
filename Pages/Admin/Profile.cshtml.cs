using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Pages.Admin
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ProfileModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public User CurrentUser { get; set; }

        [BindProperty]
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [BindProperty]
        [Phone]
        [StringLength(25)]
        public string PhoneNumber { get; set; }

        [BindProperty]
        [StringLength(50)]
        public string Pronouns { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        public string? PasswordSuccessMessage { get; set; }
        public string? PasswordErrorMessage { get; set; }

        private bool IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;
            return (from ur in _db.UserRoles
                    join r in _db.Roles on ur.RoleId equals r.RoleId
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

            CurrentUser = _db.Users.Find(userId.Value);
            if (CurrentUser == null)
                return RedirectToPage("/Account/Login");

            FullName    = CurrentUser.FullName;
            PhoneNumber = CurrentUser.PhoneNumber;
            Pronouns    = CurrentUser.Pronouns;

            return Page();
        }

        public IActionResult OnPost()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Account/Login");

            if (!IsAdmin())
                return RedirectToPage("/Index");

            if (!ModelState.IsValid)
            {
                CurrentUser = _db.Users.Find(userId.Value);
                return Page();
            }

            var user = _db.Users.Find(userId.Value);
            if (user == null)
                return RedirectToPage("/Account/Login");

            user.FullName    = FullName;
            user.PhoneNumber = PhoneNumber;
            user.Pronouns    = Pronouns;

            _db.SaveChanges();

            HttpContext.Session.SetString("UserName", user.FullName);

            SuccessMessage = "Profile updated successfully!";
            CurrentUser = user;
            return Page();
        }

        public IActionResult OnPostChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Account/Login");

            if (!IsAdmin())
                return RedirectToPage("/Index");

            CurrentUser = _db.Users.Find(userId.Value);
            if (CurrentUser == null)
                return RedirectToPage("/Account/Login");

            FullName    = CurrentUser.FullName;
            PhoneNumber = CurrentUser.PhoneNumber;
            Pronouns    = CurrentUser.Pronouns;

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                PasswordErrorMessage = "New password must be at least 6 characters.";
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                PasswordErrorMessage = "Passwords do not match. Please try again.";
                return Page();
            }

            CurrentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            _db.SaveChanges();

            PasswordSuccessMessage = "Password changed successfully.";
            NewPassword     = null;
            ConfirmPassword = null;
            return Page();
        }
    }
}
