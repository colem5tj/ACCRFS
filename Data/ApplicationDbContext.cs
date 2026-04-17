using Microsoft.EntityFrameworkCore;
using ACC_Demo.Models;

namespace ACC_Demo.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<SkillVerification> SkillVerifications => Set<SkillVerification>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Media> MediaItems => Set<Media>();
    public DbSet<UserLocationPreference> UserLocationPreferences => Set<UserLocationPreference>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TimeLedger> TimeLedgers => Set<TimeLedger>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Feedback> FeedbackItems => Set<Feedback>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<AdminReport> AdminReports => Set<AdminReport>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Member" },
            new Role { RoleId = 3, RoleName = "OrganizationRep" }
        );

        // ── Decimal precision ──────────────────────────────────────────
        modelBuilder.Entity<Request>()
            .Property(r => r.HoursNeeded)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<Transaction>()
            .Property(t => t.HoursTransferred)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<TimeLedger>()
            .Property(t => t.HoursChange)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<TimeLedger>()
            .Property(t => t.BalanceAfter)
            .HasColumnType("decimal(6,2)");

        modelBuilder.Entity<User>()
            .Property(u => u.CurrentBalance)
            .HasColumnType("decimal(6,2)");
        modelBuilder.Entity<UserLocationPreference>()
        .Property(l => l.SearchRadiusMiles)
        .HasColumnType("decimal(8,2)");

        modelBuilder.Entity<UserLocationPreference>()
            .Property(l => l.ApproxLatitude)
            .HasColumnType("decimal(9,6)");

        modelBuilder.Entity<UserLocationPreference>()
            .Property(l => l.ApproxLongitude)
            .HasColumnType("decimal(9,6)");

        // ── UserRole ───────────────────────────────────────────────────
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Transaction (two FKs to Users) ────────────────────────────
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Provider)
            .WithMany(u => u.TransactionsProvided)
            .HasForeignKey(t => t.ProviderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Receiver)
            .WithMany(u => u.TransactionsReceived)
            .HasForeignKey(t => t.ReceiverId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── AdminReport (two FKs to Users) ────────────────────────────
        modelBuilder.Entity<AdminReport>()
            .HasOne(a => a.Reporter)
            .WithMany()
            .HasForeignKey(a => a.ReporterId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AdminReport>()
            .HasOne(a => a.ReportedUser)
            .WithMany()
            .HasForeignKey(a => a.ReportedUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Message (two FKs to Users, optional FK to Transaction) ───
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Transaction)
            .WithMany(t => t.Messages)
            .HasForeignKey(m => m.TransactionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ── UserBlock (two FKs to Users) ──────────────────────────────
        modelBuilder.Entity<UserBlock>()
            .HasOne(b => b.BlockerUser)
            .WithMany()
            .HasForeignKey(b => b.BlockerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserBlock>()
            .HasOne(b => b.BlockedUser)
            .WithMany()
            .HasForeignKey(b => b.BlockedUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Request (FK to Users) ─────────────────────────────────────
        modelBuilder.Entity<Request>()
            .HasOne(r => r.CreatedByUser)
            .WithMany(u => u.RequestsCreated)
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── TimeLedger (FK to Users) ──────────────────────────────────
        modelBuilder.Entity<TimeLedger>()
            .HasOne(l => l.User)
            .WithMany(u => u.LedgerEntries)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Feedback (FK to Users) ────────────────────────────────────
        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.GivenByUser)
            .WithMany()
            .HasForeignKey(f => f.GivenByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Notification (FK to Users) ────────────────────────────────
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── OrganizationMember (FK to Users) ──────────────────────────
        modelBuilder.Entity<OrganizationMember>()
            .HasOne(om => om.User)
            .WithMany()
            .HasForeignKey(om => om.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Organization (FK to Users) ────────────────────────────────
        modelBuilder.Entity<Organization>()
            .HasOne(o => o.CreatedByUser)
            .WithMany()
            .HasForeignKey(o => o.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Media (FK to Users) ───────────────────────────────────────
        modelBuilder.Entity<Media>()
            .HasOne(m => m.User)
            .WithMany(u => u.MediaItems)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── UserLocationPreference (FK to Users) ──────────────────────
        modelBuilder.Entity<UserLocationPreference>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Availability (FK to Users) ────────────────────────────────
        modelBuilder.Entity<Availability>()
            .HasOne(a => a.User)
            .WithMany(u => u.Availabilities)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── UserSkill (FK to Users) ───────────────────────────────────
        modelBuilder.Entity<UserSkill>()
            .HasOne(us => us.User)
            .WithMany(u => u.UserSkills)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Request>()
            .HasOne(r => r.AcceptedProvider)
            .WithMany()
            .HasForeignKey(r => r.AcceptedProviderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}