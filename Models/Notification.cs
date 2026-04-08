using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }

    [Required, StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
