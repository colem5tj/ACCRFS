using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class HourAllocation
{
    public int HourAllocationId { get; set; }

    public int? UserId { get; set; }
    public int? OrganizationId { get; set; }

    public decimal HoursPerPeriod { get; set; }

    [StringLength(10)]
    public string PeriodType { get; set; } = "Weekly"; // "Weekly" or "Monthly"

    public bool IsActive { get; set; } = true;

    public int CreatedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastProcessedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public User? User { get; set; }
    public Organization? Organization { get; set; }
    public User? CreatedByAdmin { get; set; }
}
