using ACC_Demo.Models;

namespace ACC_Demo.Models;

public class OrganizationMember
{
    public int OrganizationMemberId { get; set; }
    public int OrganizationId { get; set; }
    public int UserId { get; set; }
    public string RoleInOrg { get; set; } = "Coordinator";

    public Organization? Organization { get; set; }
    public User? User { get; set; }
}
