using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Member
{
    public class BrowseRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public BrowseRequestsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Request> Requests { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SkillFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool UrgentOnly { get; set; }

        public void OnGet()
        {
            var query = _db.Requests.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(r =>
                    r.Title.Contains(SearchTerm) ||
                    r.Description.Contains(SearchTerm));
            }

            if (!string.IsNullOrEmpty(SkillFilter))
            {
                query = query.Where(r => r.Title.Contains(SkillFilter) ||
                                         r.Description.Contains(SkillFilter));
            }

            if (UrgentOnly)
            {
                query = query.Where(r => r.UrgencyLevel == "High");
            }

            Requests = query.OrderByDescending(r => r.CreatedAt).ToList();
        }
    }
}