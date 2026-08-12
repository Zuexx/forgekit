using System.Linq.Expressions;
using System.Text;
using Anvil.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Anvil.Data;

/// <summary>
/// Base context supplying the model and persistence conventions that are identical
/// across every product built on the shared layer.
/// </summary>
/// <remarks>
/// Products derive from this type, declare their own DbSets, and override
/// <see cref="ConfigureProductModel"/> for relationships, indexes, and column types.
/// The ordering in <see cref="OnModelCreating"/> is deliberate: soft-delete filters are
/// applied first, product configuration second, and the camelCase naming convention last
/// so explicit entity and column configuration is applied before names are rewritten.
/// </remarks>
public abstract class PlatformDbContext : DbContext
{
    protected PlatformDbContext(DbContextOptions options) : base(options)
    {
    }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplySoftDeleteFilters(modelBuilder);

        ConfigureProductModel(modelBuilder);

        // Must run last so explicit entity and column configuration is applied first.
        ApplyCamelCaseNames(modelBuilder);
    }

    /// <summary>
    /// Product-specific model configuration: relationships, indexes, and column types.
    /// </summary>
    protected virtual void ConfigureProductModel(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Applies a global query filter excluding soft-deleted rows to every entity
    /// implementing <see cref="ISoftDelete"/>.
    /// </summary>
    /// <remarks>
    /// Derived by reflection rather than listed per entity, so a product entity gains the
    /// filter by implementing the interface and cannot be forgotten.
    /// </remarks>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeleted = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(isDeleted), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    /// <summary>
    /// Applies lowerCamelCase naming to tables, schemas, columns, indexes, keys, and
    /// foreign-key constraints.
    /// </summary>
    /// <remarks>
    /// Changing this convention affects EF migrations and may produce rename operations;
    /// review generated migrations carefully.
    /// </remarks>
    private static void ApplyCamelCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Use the CLR type (singular) to match existing table names.
            entity.SetTableName(ToCamelCase(entity.ClrType.Name));

            var schema = entity.GetSchema();
            if (!string.IsNullOrEmpty(schema))
                entity.SetSchema(ToCamelCase(schema));

            foreach (var prop in entity.GetProperties())
                prop.SetColumnName(ToCamelCase(prop.GetColumnName() ?? prop.Name));

            foreach (var idx in entity.GetIndexes())
            {
                var idxName = idx.GetDatabaseName() ?? idx.Name ?? string.Empty;
                idx.SetDatabaseName(ToCamelCase(idxName));
            }

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName() ?? $"pk_{entity.ClrType.Name}";
                key.SetName(ToCamelCase(keyName));
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                var fkName = fk.GetConstraintName() ?? $"fk_{entity.ClrType.Name}";
                fk.SetConstraintName(ToCamelCase(fkName));
            }
        }
    }

    private static string ToCamelCase(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        var parts = name.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var s = parts[0];
            if (s.Length == 1) return s.ToLowerInvariant();
            return char.ToLowerInvariant(s[0]) + s[1..];
        }

        var sb = new StringBuilder();
        sb.Append(parts[0].ToLowerInvariant());
        for (int i = 1; i < parts.Length; i++)
        {
            var p = parts[i];
            if (string.IsNullOrEmpty(p)) continue;
            sb.Append(char.ToUpperInvariant(p[0]));
            if (p.Length > 1) sb.Append(p[1..]);
        }
        return sb.ToString();
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stamps timestamps and version on tracked auditable entities.
    /// CreatedBy and UpdatedBy are set by the application layer, not here.
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (IAuditableEntity)entry.Entity;
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = now;
                entity.Version++;
            }
        }
    }
}
