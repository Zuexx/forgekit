using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Auth;

[Table("account")]
[Index(nameof(UserId))]
public class Account
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = null!;

    [Column("accountId")]
    public string AccountId { get; set; } = null!;

    [Column("providerId")]
    public string ProviderId { get; set; } = null!;

    [Column("userId")]
    [MaxLength(36)]
    public string UserId { get; set; } = null!;

    [Column("accessToken")]
    public string? AccessToken { get; set; }

    [Column("refreshToken")]
    public string? RefreshToken { get; set; }

    [Column("idToken")]
    public string? IdToken { get; set; }

    [Column("accessTokenExpiresAt")]
    public DateTime? AccessTokenExpiresAt { get; set; }

    [Column("refreshTokenExpiresAt")]
    public DateTime? RefreshTokenExpiresAt { get; set; }

    [Column("scope")]
    public string? Scope { get; set; }

    [Column("password")]
    public string? Password { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
