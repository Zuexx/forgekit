using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForgeKit.Api.Entities.Auth;

[Table("jwks")]
public class Jwk
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = null!;

    [Column("publicKey")]
    public string PublicKey { get; set; } = null!;

    [Column("privateKey")]
    public string PrivateKey { get; set; } = null!;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}
