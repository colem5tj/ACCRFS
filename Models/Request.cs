using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class Request
{
    public int RequestId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? OrganizationId { get; set; }
    public int? SkillId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public decimal HoursNeeded { get; set; }

    [StringLength(20)]
    public string UrgencyLevel { get; set; } = "Medium"; // Low, Medium, High

    public bool IsEmergency { get; set; } = false;
    public DateTime? ScheduledDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Open"; // Open, Matched, InProgress, Completed, Cancelled, PendingApproval

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? CreatedByUser { get; set; }
    public Organization? Organization { get; set; }
    public Skill? Skill { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public int? AcceptedProviderId { get; set; }
    public User? AcceptedProvider { get; set; }
}
