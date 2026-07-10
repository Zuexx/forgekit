using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Auth;

[Table("session")]
[Index(nameof(Token), IsUnique = true)]
[Index(nameof(UserId))]
public class Session
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = null!;

    [Column("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [Column("token")]
    [MaxLength(255)]
    public string Token { get; set; } = null!;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [Column("ipAddress")]
    public string? IpAddress { get; set; }

    [Column("userAgent")]
    public string? UserAgent { get; set; }

    [Column("userId")]
    [MaxLength(36)]
    public string UserId { get; set; } = null!;

    [Column("impersonatedBy")]
    public string? ImpersonatedBy { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
