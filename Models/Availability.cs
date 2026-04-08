using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class Availability
{
    public int AvailabilityId { get; set; }
    public int UserId { get; set; }

    [Required, StringLength(20)]
    public string DayOfWeek { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public User? User { get; set; }
}
