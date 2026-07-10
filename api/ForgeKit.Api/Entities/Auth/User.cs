using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Auth;

[Table("user")]
[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = null!;

    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Column("emailVerified")]
    public bool EmailVerified { get; set; }

    [Column("image")]
    public string? Image { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [Column("role")]
    public string? Role { get; set; }

    [Column("banned")]
    public bool? Banned { get; set; }

    [Column("banReason")]
    public string? BanReason { get; set; }

    [Column("banExpires")]
    public DateTime? BanExpires { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
