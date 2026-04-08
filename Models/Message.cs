using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class Message
{
    public int MessageId { get; set; }
    public int TransactionId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }

    [Required, StringLength(1500)]
    public string Content { get; set; } = string.Empty;

    public bool IsContactAnonymized { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Transaction? Transaction { get; set; }
    public User? Sender { get; set; }
    public User? Receiver { get; set; }
}
