
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Passall.Modeles;

public class DBUserProfile
{
    [Key]
    public Guid Id { get; set; }

    public string Login { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Url { get; set; }
    public string Password { get; set; }

    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public DBUser User { get; set; }

    public Guid CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    public DBProfileCategory Category { get; set; }

    [NotMapped] public int BreachCount { get; set; }
    [NotMapped] public bool IsBreached { get; set; }
    [NotMapped] public bool IsSafe { get; set; }

    // Affichés uniquement en mode super-admin.
    [NotMapped] public bool ShowOwner { get; set; }
    [NotMapped] public string OwnerLabel { get; set; } = string.Empty;
}
