    using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class Transaction
{
    public int TransactionId { get; set; }
    public int RequestId { get; set; }
    public int ProviderId { get; set; }
    public int ReceiverId { get; set; }
    public int? OrganizationId { get; set; }

    public decimal HoursTransferred { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Approved, Disputed

    [StringLength(100)]
    public string? ContactType { get; set; }

    public int? VerificationPin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    public Request? Request { get; set; }
    public User? Provider { get; set; }
    public User? Receiver { get; set; }
    public Organization? Organization { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Feedback> FeedbackItems { get; set; } = new List<Feedback>();
}
