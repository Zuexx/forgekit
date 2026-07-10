using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Configuration
{
    /// <summary>
    /// Represents a label that can be attached to categories within a workspace
    /// </summary>
    [Table("Labels")]
    [Index(nameof(LabelCode), IsUnique = true)]
    [Index(nameof(WorkspaceId))]
    public class Label : BaseEntity
    {
        /// <summary>
        /// ID of the workspace this label belongs to
        /// </summary>
        [Required]
        [MaxLength(32)]
        public required string WorkspaceId { get; set; }

        /// <summary>
        /// Unique code identifying the label within the workspace
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string LabelCode { get; set; }

        /// <summary>
        /// Display name of the label
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string LabelName { get; set; }

        /// <summary>
        /// Hex color code for the label, e.g. "#3B82F6"
        /// </summary>
        [MaxLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// Indicates whether the label is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Workspace that owns this label.
        /// </summary>
        [ForeignKey(nameof(WorkspaceId))]
        public virtual Workspace? Workspace { get; set; }

        /// <summary>
        /// Category-label join records linking this label to categories
        /// </summary>
        public virtual ICollection<CategoryLabel> CategoryLabels { get; set; } = new List<CategoryLabel>();
    }
}
