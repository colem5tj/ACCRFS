using ACC_Demo.Models;

namespace ACC_Demo.Models;

public class Feedback
{
    public int FeedbackId { get; set; }
    public int TransactionId { get; set; }
    public int GivenByUserId { get; set; }
    public bool IsCheerful { get; set; }
    public bool IsEfficient { get; set; }
    public bool IsOnTime { get; set; }
    public bool IsHighlySkilled { get; set; }

    public Transaction? Transaction { get; set; }
    public User? GivenByUser { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }
}