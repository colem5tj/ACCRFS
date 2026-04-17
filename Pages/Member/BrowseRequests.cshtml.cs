using System.Text;
using System.Text.Json;
using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ACC_Demo.Pages.Member
{
    public class BrowseRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public BrowseRequestsModel(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public List<Request> Requests { get; set; } = new();
        public List<RequestVm> RecommendedRequests { get; set; } = new();
        public string CurrentUserBio { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SkillFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool UrgentOnly { get; set; }

        public async Task OnGetAsync()
        {
            var query = _db.Requests.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
                query = query.Where(r => r.Title.Contains(SearchTerm) || r.Description.Contains(SearchTerm));

            if (!string.IsNullOrEmpty(SkillFilter))
                query = query.Where(r => r.Title.Contains(SkillFilter) || r.Description.Contains(SkillFilter));

            if (UrgentOnly)
                query = query.Where(r => r.UrgencyLevel == "High");

            Requests = query.OrderByDescending(r => r.CreatedAt).ToList();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                CurrentUserBio = _db.Users
                    .Where(u => u.UserId == userId.Value)
                    .Select(u => u.Bio ?? string.Empty)
                    .FirstOrDefault() ?? string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(CurrentUserBio) && Requests.Any())
                RecommendedRequests = await GetRecommendedRequestsAsync();
        }

        private async Task<List<RequestVm>> GetRecommendedRequestsAsync()
        {
            var endpoint = _config["AzureOpenAI:Endpoint"];
            var apiKey = _config["AzureOpenAI:ApiKey"];
            var deployment = _config["AzureOpenAI:DeploymentName"];

            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(deployment))
                return new List<RequestVm>();

            var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version=2024-10-21";

            var requestsJson = JsonSerializer.Serialize(
                Requests.Select(r => new RequestVm
                {
                    Id = r.RequestId,
                    Title = r.Title,
                    Description = r.Description,
                    HoursNeeded = (int)r.HoursNeeded
                })
            );

            var prompt = $@"
You are a recommendation engine.

User bio:
{CurrentUserBio}

Available requests:
{requestsJson}

Return ONLY a JSON array of the BEST matching requests.
Select up to 3.
Return EXACT objects from the list (no modifications).

Format:
[
{{ ""Id"": 1, ""Title"": ""..."", ""Description"": ""..."", ""HoursNeeded"": 2 }}
]
";

            var body = new
            {
                messages = new object[]
                {
                    new { role = "system", content = "You return only JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2,
                max_tokens = 300
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await client.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new List<RequestVm>();

                using var doc = JsonDocument.Parse(result);

                var raw = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return JsonSerializer.Deserialize<List<RequestVm>>(raw ?? "[]") ?? new List<RequestVm>();
            }
            catch
            {
                return new List<RequestVm>();
            }
        }

        public class RequestVm
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int HoursNeeded { get; set; }
        }
    }
}
