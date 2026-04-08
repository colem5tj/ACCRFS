using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class Media
{
    public int MediaId { get; set; }
    public int UserId { get; set; }

    [Required, StringLength(50)]
    public string Type { get; set; } = string.Empty; // profile_photo, avatar, org_logo, license_doc

    [Required, StringLength(255)]
    public string MediaUrl { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public User? User { get; set; }
}
