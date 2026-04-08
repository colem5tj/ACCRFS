using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class Skill
{
    public int SkillId { get; set; }

    [Required, StringLength(100)]
    public string SkillName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Category { get; set; }

    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}
