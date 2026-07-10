using Microsoft.EntityFrameworkCore;
using ForgeKit.Api.Entities.Analytics;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Entities.Configuration;
using ForgeKit.Api.Entities.Core;
using ForgeKit.Api.Entities.Todos;
using System;
using System.Text;

namespace ForgeKit.Api.Data
{
    /// <summary>
    /// Application Database Context
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Core Entities
        public DbSet<Workspace> Workspaces { get; set; } = null!;
        public DbSet<Member> Members { get; set; } = null!;

        // Configuration Entities
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Label> Labels { get; set; } = null!;
        public DbSet<CategoryLabel> CategoryLabels { get; set; } = null!;

        // Sample Todo Entities
        public DbSet<TodoItem> TodoItems { get; set; } = null!;
        public DbSet<TodoStatusHistory> TodoStatusHistory { get; set; } = null!;

        // Analytics Entities
        public DbSet<WorkspaceAnalytics> WorkspaceAnalytics { get; set; } = null!;
        public DbSet<DailyActivitySnapshot> DailyActivitySnapshots { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply global query filters for soft delete
            ConfigureSoftDeleteFilters(modelBuilder);

            // Configure relationships
            ConfigureRelationships(modelBuilder);

            // Configure indexes
            ConfigureIndexes(modelBuilder);

            // Configure JSON columns
            ConfigureJsonColumns(modelBuilder);

            // Apply camelCase naming convention for relational objects.
            // Call last so explicit entity/column configs are applied first.
            ConfigureCamelCaseNames(modelBuilder);
        }

        /// <summary>
        /// Configure soft delete global query filters
        /// </summary>
        private void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Workspace>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Member>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Label>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CategoryLabel>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<TodoItem>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<TodoStatusHistory>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<WorkspaceAnalytics>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<DailyActivitySnapshot>().HasQueryFilter(e => !e.IsDeleted);
        }

        /// <summary>
        /// Configure entity relationships
        /// </summary>
        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            // Category self-reference
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.ChildCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Category → Workspace (many-to-one, optional global category)
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Workspace)
                .WithMany(w => w.Categories)
                .HasForeignKey(c => c.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Label → Workspace (many-to-one)
            modelBuilder.Entity<Label>()
                .HasOne(l => l.Workspace)
                .WithMany(w => w.Labels)
                .HasForeignKey(l => l.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Category ↔ Label many-to-many via CategoryLabel
            modelBuilder.Entity<CategoryLabel>()
                .HasOne(cl => cl.Category)
                .WithMany(c => c.CategoryLabels)
                .HasForeignKey(cl => cl.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CategoryLabel>()
                .HasOne(cl => cl.Label)
                .WithMany(l => l.CategoryLabels)
                .HasForeignKey(cl => cl.LabelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Member → Workspace (many-to-one)
            modelBuilder.Entity<Member>()
                .HasOne(m => m.Workspace)
                .WithMany(w => w.Members)
                .HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkspaceAnalytics → Workspace (many-to-one)
            modelBuilder.Entity<WorkspaceAnalytics>()
                .HasOne(wa => wa.Workspace)
                .WithMany(w => w.Analytics)
                .HasForeignKey(wa => wa.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // DailyActivitySnapshot → Workspace (many-to-one)
            modelBuilder.Entity<DailyActivitySnapshot>()
                .HasOne(d => d.Workspace)
                .WithMany()
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // TodoItem → Workspace (many-to-one)
            modelBuilder.Entity<TodoItem>()
                .HasOne(t => t.Workspace)
                .WithMany(w => w.TodoItems)
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // TodoItem → Member (many-to-one, optional assignee)
            modelBuilder.Entity<TodoItem>()
                .HasOne(t => t.AssignedTo)
                .WithMany(m => m.AssignedTodoItems)
                .HasForeignKey(t => t.AssignedToMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // TodoItem → Category (many-to-one, optional category)
            modelBuilder.Entity<TodoItem>()
                .HasOne(t => t.Category)
                .WithMany(c => c.TodoItems)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // TodoStatusHistory → TodoItem (many-to-one)
            modelBuilder.Entity<TodoStatusHistory>()
                .HasOne(h => h.TodoItem)
                .WithMany(t => t.StatusHistory)
                .HasForeignKey(h => h.TodoItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <summary>
        /// Configure additional indexes for analytics performance
        /// </summary>
        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.WorkspaceId, c.IsDeleted });

            modelBuilder.Entity<WorkspaceAnalytics>()
                .HasIndex(wa => new { wa.PeriodStart, wa.WorkspaceId, wa.IsDeleted });

            modelBuilder.Entity<DailyActivitySnapshot>()
                .HasIndex(d => new { d.SnapshotDate, d.WorkspaceId, d.IsDeleted });

            modelBuilder.Entity<TodoItem>()
                .HasIndex(t => new { t.WorkspaceId, t.CurrentStatus, t.IsDeleted });

            modelBuilder.Entity<TodoStatusHistory>()
                .HasIndex(h => new { h.TodoItemId, h.Timestamp });
        }

        /// <summary>
        /// Configure JSON columns for flexible metadata
        /// </summary>
        private void ConfigureJsonColumns(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkspaceAnalytics>()
                .Property(wa => wa.MetricsJson)
                .HasColumnType("text");

            modelBuilder.Entity<TodoItem>()
                .Property(t => t.MetadataJson)
                .HasColumnType("text");

            // Configure decimal precisions to avoid silent truncation on SQL Server
            modelBuilder.Entity<WorkspaceAnalytics>()
                .Property(wa => wa.AverageCompletionDays)
                .HasPrecision(18, 2);
        }

        /// <summary>
        /// Apply camelCase naming convention for relational database objects.
        ///
        /// Converts table names, schema, column names, index names, key names and
        /// foreign-key constraint names to lowerCamelCase (start lower-case; subsequent
        /// words capitalized). Note: changing naming conventions will affect EF Migrations
        /// (may produce rename operations) so review generated migrations carefully.
        /// </summary>
        /// <param name="modelBuilder">The EF Core model builder.</param>
        private void ConfigureCamelCaseNames(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Table and schema
                // Use CLR type (singular) to match existing DB table names (e.g. 'region')
                var tableName = entity.ClrType.Name;
                entity.SetTableName(ToCamelCase(tableName));

                var schema = entity.GetSchema();
                if (!string.IsNullOrEmpty(schema))
                    entity.SetSchema(ToCamelCase(schema));

                // Columns
                foreach (var prop in entity.GetProperties())
                    prop.SetColumnName(ToCamelCase(prop.GetColumnName() ?? prop.Name));

                // Index names
                foreach (var idx in entity.GetIndexes())
                {
                    var idxName = idx.GetDatabaseName() ?? idx.Name ?? string.Empty;
                    idx.SetDatabaseName(ToCamelCase(idxName));
                }

                // Keys
                foreach (var key in entity.GetKeys())
                {
                    var keyName = key.GetName() ?? $"pk_{entity.ClrType.Name}";
                    key.SetName(ToCamelCase(keyName));
                }

                // Foreign key constraint names
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

            var parts = name.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                var s = parts[0];
                if (s.Length == 1) return s.ToLowerInvariant();
                return char.ToLowerInvariant(s[0]) + s.Substring(1);
            }

            var sb = new StringBuilder();
            sb.Append(parts[0].ToLowerInvariant());
            for (int i = 1; i < parts.Length; i++)
            {
                var p = parts[i];
                if (string.IsNullOrEmpty(p)) continue;
                sb.Append(char.ToUpperInvariant(p[0]));
                if (p.Length > 1) sb.Append(p.Substring(1));
            }
            return sb.ToString();
        }



        /// <summary>
        /// Override SaveChanges to automatically update audit fields
        /// </summary>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to automatically update audit fields
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Automatically update audit fields on save
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
                    // CreatedBy and UpdatedBy should be set by the application layer
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = now;
                    entity.Version++;
                    // UpdatedBy should be set by the application layer
                }
            }
        }
    }
}
