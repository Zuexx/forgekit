using Anvil.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Anvil.Data.Auth;

public class BetterAuthDbContext : DbContext
{
    public BetterAuthDbContext(DbContextOptions<BetterAuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Verification> Verifications { get; set; } = null!;
    public DbSet<Jwk> Jwks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Keeps Better Auth's tables out of the same namespace as the product's tables when
        // both share one database instance (Postgres, SQL Server). EF Core's SQLite provider
        // accepts this call but does not honour it — SQLite has no schema concept, and the
        // two DbContexts' table names do not collide, so one flat file is still "separate".
        modelBuilder.HasDefaultSchema("auth");

        modelBuilder.Entity<Account>()
            .HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Session>()
            .HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
