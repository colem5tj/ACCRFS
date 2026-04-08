using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class SkillVerification
{
    public int SkillVerificationId { get; set; }
    public int UserSkillId { get; set; }

    [StringLength(100)]
    public string? LicenseNumber { get; set; }

    [StringLength(255)]
    public string? DocumentUrl { get; set; }

    public bool IsVerified { get; set; }
    public int? VerifiedByAdminId { get; set; }
    public DateTime? VerificationDate { get; set; }

    public UserSkill? UserSkill { get; set; }
}
