using ACC_Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class Role
{
    public int RoleId { get; set; }

    [Required, StringLength(50)]
    public string RoleName { get; set; } = string.Empty; // Admin, Member, OrganizationRep

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
