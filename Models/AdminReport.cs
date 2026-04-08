using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class AdminReport
{
    public int AdminReportId { get; set; }
    public int ReporterId { get; set; }
    public int ReportedUserId { get; set; }

    [Required, StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Reporter { get; set; }
    public User? ReportedUser { get; set; }
}
