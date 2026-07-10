using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Analytics
{
    /// <summary>
    /// Aggregated analytics record for a workspace over a given time period
    /// </summary>
    [Table("WorkspaceAnalytics")]
    [Index(nameof(WorkspaceId), nameof(PeriodStart))]
    [Index(nameof(PeriodStart), nameof(PeriodEnd))]
    public class WorkspaceAnalytics : BaseEntity
    {
        /// <summary>
        /// ID of the workspace these analytics belong to
        /// </summary>
        [Required]
        [MaxLength(32)]
        public required string WorkspaceId { get; set; }

        /// <summary>
        /// Denormalized workspace name at the time the record was captured
        /// </summary>
        [Required]
        [MaxLength(200)]
        public required string WorkspaceName { get; set; }

        /// <summary>
        /// Start of the analytics period (inclusive)
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// End of the analytics period (inclusive)
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Total number of todo items in the workspace during the period
        /// </summary>
        public int TotalTodos { get; set; }

        /// <summary>
        /// Number of todo items completed during the period
        /// </summary>
        public int CompletedTodos { get; set; }

        /// <summary>
        /// Number of todo items that became overdue during the period
        /// </summary>
        public int OverdueTodos { get; set; }

        /// <summary>
        /// Number of todo items cancelled during the period
        /// </summary>
        public int CancelledTodos { get; set; }

        /// <summary>
        /// Average number of days to complete a todo item during the period.
        /// Configure precision in DbContext: modelBuilder.Entity&lt;WorkspaceAnalytics&gt;().Property(x =&gt; x.AverageCompletionDays).HasPrecision(18, 2)
        /// </summary>
        public decimal? AverageCompletionDays { get; set; }

        /// <summary>
        /// Number of members who were active during the period
        /// </summary>
        public int ActiveMembers { get; set; }

        /// <summary>
        /// Extended metrics stored as a JSON string for additional analytical data
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? MetricsJson { get; set; }

        /// <summary>
        /// The workspace these analytics are associated with
        /// </summary>
        [ForeignKey(nameof(WorkspaceId))]
        public virtual Workspace? Workspace { get; set; }
    }
}
