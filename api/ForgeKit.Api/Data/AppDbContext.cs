using Anvil.Data;
using ForgeKit.Api.Entities.Analytics;
using ForgeKit.Api.Entities.Configuration;
using ForgeKit.Api.Entities.Core;
using ForgeKit.Api.Entities.Todos;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Data
{
    /// <summary>
    /// Application Database Context
    /// </summary>
    public class AppDbContext : PlatformDbContext
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

        /// <summary>
        /// Product-specific model configuration. Soft-delete filters and the camelCase
        /// naming convention are applied by <see cref="PlatformDbContext"/>.
        /// </summary>
        protected override void ConfigureProductModel(ModelBuilder modelBuilder)
        {
            ConfigureRelationships(modelBuilder);
            ConfigureIndexes(modelBuilder);
            ConfigureJsonColumns(modelBuilder);
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

    }
}
