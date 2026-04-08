using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Pages.Member
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
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; }

        [BindProperty]
        [Phone]
        [StringLength(25)]
        public string PhoneNumber { get; set; }

        [BindProperty]
        [StringLength(50)]
        public string Pronouns { get; set; }

        [BindProperty]
        [StringLength(100)]
        public string Occupation { get; set; }

        [BindProperty]
        [StringLength(1000)]
        public string Bio { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Account/Login");

            CurrentUser = _db.Users.Find(userId.Value);
            if (CurrentUser == null)
                return RedirectToPage("/Account/Login");

            FullName = CurrentUser.FullName;
            Email = CurrentUser.Email;
            PhoneNumber = CurrentUser.PhoneNumber;
            Pronouns = CurrentUser.Pronouns;
            Occupation = CurrentUser.Occupation;
            Bio = CurrentUser.Bio;
            

            return Page();
        }

        public IActionResult OnPost()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
            {
                CurrentUser = _db.Users.Find(userId.Value);
                return Page();
            }

            var user = _db.Users.Find(userId.Value);
            if (user == null)
                return RedirectToPage("/Account/Login");

            user.FullName = FullName;
            user.Email = Email;
            user.PhoneNumber = PhoneNumber;
            user.Pronouns = Pronouns;
            user.Occupation = Occupation;
            user.Bio = Bio;
            

            _db.SaveChanges();

            HttpContext.Session.SetString("UserName", user.FullName);

            SuccessMessage = "Profile updated successfully!";
            CurrentUser = user;
            return Page();
        }
    }
}