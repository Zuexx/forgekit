using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Analytics
{
    /// <summary>
    /// A point-in-time daily snapshot of activity metrics for a workspace
    /// </summary>
    [Table("DailyActivitySnapshots")]
    [Index(nameof(SnapshotDate), nameof(WorkspaceId))]
    [Index(nameof(SnapshotDate))]
    public class DailyActivitySnapshot : BaseEntity
    {
        /// <summary>
        /// The calendar date this snapshot represents
        /// </summary>
        public DateTime SnapshotDate { get; set; }

        /// <summary>
        /// ID of the workspace this snapshot belongs to
        /// </summary>
        [Required]
        [MaxLength(32)]
        public required string WorkspaceId { get; set; }

        /// <summary>
        /// Number of todo items created on this date
        /// </summary>
        public int TodosCreated { get; set; }

        /// <summary>
        /// Number of todo items completed on this date
        /// </summary>
        public int TodosCompleted { get; set; }

        /// <summary>
        /// Number of todo items deleted on this date
        /// </summary>
        public int TodosDeleted { get; set; }

        /// <summary>
        /// Number of members who performed at least one action on this date
        /// </summary>
        public int ActiveMembers { get; set; }

        /// <summary>
        /// The workspace this snapshot belongs to
        /// </summary>
        [ForeignKey(nameof(WorkspaceId))]
        public virtual Workspace? Workspace { get; set; }
    }
}
