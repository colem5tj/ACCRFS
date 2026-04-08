using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Pages.Member
{
    public class CreateRequestModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CreateRequestModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required(ErrorMessage = "Request title is required")]
        [StringLength(150)]
        public string RequestTitle { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Detailed description is required")]
        [StringLength(2000)]
        public string DetailedDescription { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Primary skill is required")]
        public string PrimarySkill { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Desired start date is required")]
        [DataType(DataType.Date)]
        public DateTime DesiredStartDate { get; set; }

        [BindProperty]
        [DataType(DataType.Time)]
        public TimeSpan TimeSlot { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Estimated hours is required")]
        [Range(1, 100)]
        public int EstimatedHours { get; set; }

        [BindProperty]
        public bool MarkAsUrgent { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var request = new ACC_Demo.Models.Request
            {
                CreatedByUserId = userId.Value,
                Title = RequestTitle,
                Description = DetailedDescription,
                HoursNeeded = EstimatedHours,
                UrgencyLevel = MarkAsUrgent ? "High" : "Medium",
                ScheduledDate = DesiredStartDate,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            _db.Requests.Add(request);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Your help request has been submitted successfully!";
            return RedirectToPage("/Member/MyRequests");
        }
    }
}