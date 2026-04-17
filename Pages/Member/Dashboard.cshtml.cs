using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using ACC_Demo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Pages.Member;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public DashboardModel(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public decimal CurrentBalance { get; set; }
    public List<RequestCardVm> ActiveRequests { get; set; } = new();
    public List<TransactionVm> AcceptedTransactions { get; set; } = new();
    public List<LedgerVm> RecentLedger { get; set; } = new();
    public int UnreadDmCount { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please enter a question.")]
    [StringLength(350, ErrorMessage = "Question cannot exceed 350 characters.")]
    public string UserQuestion { get; set; } = string.Empty;

    public string BotResponse { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToPage("/Account/Login");

        LoadDashboardData(userId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostAskAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToPage("/Account/Login");

        LoadDashboardData(userId.Value);

        if (!ModelState.IsValid)
            return Page();

        var question = (UserQuestion ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            ModelState.AddModelError(nameof(UserQuestion), "Please enter a question.");
            return Page();
        }

        BotResponse = await GetFaqBotResponseAsync(question);
        return Page();
    }

    private void LoadDashboardData(int userId)
    {
        CurrentBalance = _context.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.CurrentBalance)
            .FirstOrDefault();

        ActiveRequests = _context.Requests
            .Where(r => r.CreatedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new RequestCardVm
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Description = r.Description,
                Status = r.Status,
                HoursNeeded = r.HoursNeeded,
                UrgencyLevel = r.UrgencyLevel
            })
            .ToList();

        AcceptedTransactions = _context.Transactions
            .Include(t => t.Request)
            .Where(t => t.ProviderId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new TransactionVm
            {
                TransactionId = t.TransactionId,
                RequestTitle = t.Request != null ? t.Request.Title : "",
                Hours = t.HoursTransferred
            })
            .ToList();

        RecentLedger = _context.TimeLedgers
            .Include(l => l.Transaction)
            .ThenInclude(t => t!.Request)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new LedgerVm
            {
                Description = l.Transaction != null && l.Transaction.Request != null
                    ? l.Transaction.Request.Title
                    : "",
                HoursDelta = l.HoursChange > 0
                    ? $"+{l.HoursChange}"
                    : l.HoursChange.ToString()
            })
            .ToList();

        UnreadDmCount = _context.Messages
            .Count(m => m.TransactionId == null && m.ReceiverId == userId && !m.IsRead);
    }

    private async Task<string> GetFaqBotResponseAsync(string question)
    {
        var endpoint = _config["AzureOpenAI:Endpoint"];
        var apiKey = _config["AzureOpenAI:ApiKey"];
        var deployment = _config["AzureOpenAI:DeploymentName"];

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(deployment))
        {
            return "The FAQ assistant is not configured yet.";
        }

        var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version=2024-10-21";

        var faqContext = """
You are an FAQ assistant for a nonprofit time bank web application.

You must answer ONLY using the FAQ information below.
Do not use outside knowledge.
Do not invent policies, features, or rules.
If the answer is not clearly contained in the FAQ, respond exactly with:
I'm not able to answer that based on the available information. Please try rephrasing your question or check with an administrator for further assistance.

FAQ:
1. Earning Hours
Members earn hours by providing services to other members. The number of hours earned matches the time spent helping.

2. Spending Hours
Members use earned hours to request help from other members.

3. Hour-for-Hour Equality
One hour of service equals one hour of credit, regardless of service type.

4. Requesting Help
Members can request help by going to the Post Request page and submitting their need.

5. Offering Help
Members can offer help by browsing available requests and choosing one they can assist with.

6. Dashboard Balance
The dashboard shows the member's current hour balance.

7. Ledger
The ledger shows recent earned and spent hour transactions.

8. Commitments
Accepted commitments represent services the member has agreed to fulfill.

Keep answers concise, clear, and user-friendly.
""";

        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = faqContext },
                new { role = "user", content = question }
            },
            max_tokens = 180,
            temperature = 0.1
        };

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("api-key", apiKey);

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await client.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return "The FAQ assistant could not process that request right now.";

            using var doc = JsonDocument.Parse(result);

            return doc.RootElement
                       .GetProperty("choices")[0]
                       .GetProperty("message")
                       .GetProperty("content")
                       .GetString()?.Trim()
                   ?? "The FAQ assistant could not generate a response.";
        }
        catch
        {
            return "The FAQ assistant could not process that request right now.";
        }
    }

    public class RequestCardVm
    {
        public int RequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal HoursNeeded { get; set; }
        public string UrgencyLevel { get; set; } = string.Empty;
    }

    public class TransactionVm
    {
        public int TransactionId { get; set; }
        public string RequestTitle { get; set; } = string.Empty;
        public decimal Hours { get; set; }
    }

    public class LedgerVm
    {
        public string Description { get; set; } = string.Empty;
        public string HoursDelta { get; set; } = string.Empty;
    }
}
