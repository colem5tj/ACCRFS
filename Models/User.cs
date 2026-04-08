using System.ComponentModel.DataAnnotations;

namespace ACC_Demo.Models;

public class User
{
    public int UserId { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(25)]
    public string? PhoneNumber { get; set; }

    [Required, StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Pronouns { get; set; }

    [StringLength(100)]
    public string? Occupation { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [StringLength(100)]
    public string? AnonymousUsername { get; set; }

    public decimal CurrentBalance { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsFlagged { get; set; } = false;
    public bool IsBanned { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
    public ICollection<Media> MediaItems { get; set; } = new List<Media>();
    public ICollection<Request> RequestsCreated { get; set; } = new List<Request>();

    public ICollection<Transaction> TransactionsProvided { get; set; } = new List<Transaction>();
    public ICollection<Transaction> TransactionsReceived { get; set; } = new List<Transaction>();

    public ICollection<TimeLedger> LedgerEntries { get; set; } = new List<TimeLedger>();
}