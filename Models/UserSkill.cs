using ACC_Demo.Models;

namespace ACC_Demo.Models;

public class UserSkill
{
    public int UserSkillId { get; set; }
    public int UserId { get; set; }
    public int SkillId { get; set; }
    public string? ProficiencyLevel { get; set; }
    public bool IsProfessional { get; set; }

    public User? User { get; set; }
    public Skill? Skill { get; set; }
    public SkillVerification? SkillVerification { get; set; }
}
