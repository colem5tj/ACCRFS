using ACC_Demo.Models;

namespace ACC_Demo.Models;

public class TimeLedger
{
    public int TimeLedgerId { get; set; }
    public int UserId { get; set; }
    public int? TransactionId { get; set; }
    public string? Description { get; set; }
    public decimal HoursChange { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Transaction? Transaction { get; set; }
}
