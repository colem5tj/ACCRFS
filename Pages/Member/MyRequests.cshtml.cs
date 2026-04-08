using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Member
{
    public class MyRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public MyRequestsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Request> Requests { get; set; }

        public void OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                Requests = new List<Request>();
                return;
            }

            Requests = _db.Requests
                .Where(r => r.CreatedByUserId == userId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }
    }
}