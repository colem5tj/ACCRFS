using ACC_Demo.Models;

namespace ACC_Demo.Models;

public class UserLocationPreference
{
    public int UserLocationPreferenceId { get; set; }
    public int UserId { get; set; }
    public decimal SearchRadiusMiles { get; set; } = 25;
    public decimal? ApproxLatitude { get; set; }
    public decimal? ApproxLongitude { get; set; }
    public bool IsLocationHidden { get; set; } = true;

    [System.ComponentModel.DataAnnotations.StringLength(10)]
    public string? ZipCode { get; set; }

    public User? User { get; set; }
}
