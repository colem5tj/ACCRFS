using ACC_Demo.Models;

namespace ACC_Demo.Models;

public class UserBlock
{
    public int UserBlockId { get; set; }
    public int BlockerUserId { get; set; }
    public int BlockedUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? BlockerUser { get; set; }
    public User? BlockedUser { get; set; }
}
