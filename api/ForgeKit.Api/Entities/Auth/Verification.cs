using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Auth;

[Table("verification")]
[Index(nameof(Identifier))]
public class Verification
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = null!;

    [Column("identifier")]
    public string Identifier { get; set; } = null!;

    [Column("value")]
    public string Value { get; set; } = null!;

    [Column("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
