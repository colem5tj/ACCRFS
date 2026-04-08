using ACC_Demo.Models;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Already seeded – nothing to do
            if (context.Users.Any()) return;

            // Let SQL Server assign IDs automatically — no IDENTITY_INSERT needed
            var admin = new User
            {
                FullName     = "Alice Admin",
                Email        = "admin@demo.com",
                PasswordHash = "demo",
                Occupation   = "System Administrator",
                Bio          = "Oversees platform operations.",
                IsActive     = true,
                CurrentBalance = 0
            };
            var member = new User
            {
                FullName     = "Marcus Member",
                Email        = "member@demo.com",
                PasswordHash = "demo",
                Occupation   = "Carpenter",
                Bio          = "Happy to trade skills for time credits.",
                IsActive     = true,
                CurrentBalance = 12.5m
            };
            var orgRep = new User
            {
                FullName     = "Olivia OrgRep",
                Email        = "org@demo.com",
                PasswordHash = "demo",
                Occupation   = "Nonprofit Coordinator",
                Bio          = "Connects volunteers with community needs.",
                IsActive     = true,
                CurrentBalance = 5m
            };

            context.Users.AddRange(admin, member, orgRep);
            context.SaveChanges();

            // Roles 1-3 are seeded via HasData in OnModelCreating
            context.UserRoles.AddRange(
                new UserRole { UserId = admin.UserId,  RoleId = 1 },
                new UserRole { UserId = member.UserId, RoleId = 2 },
                new UserRole { UserId = orgRep.UserId, RoleId = 3 }
            );
            context.SaveChanges();
        }
    }
}

