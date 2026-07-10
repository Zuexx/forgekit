using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Analytics;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Entities.Configuration;
using ForgeKit.Api.Entities.Todos;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Core
{
    /// <summary>
    /// Represents a workspace that groups members and todo items
    /// </summary>
    [Table("Workspaces")]
    [Index(nameof(WorkspaceCode), IsUnique = true)]
    [Index(nameof(OwnerId))]
    public class Workspace : BaseEntity
    {
        /// <summary>
        /// Unique code identifying the workspace
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string WorkspaceCode { get; set; }

        /// <summary>
        /// Display name of the workspace
        /// </summary>
        [Required]
        [MaxLength(200)]
        public required string WorkspaceName { get; set; }

        /// <summary>
        /// Optional description of the workspace
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether the workspace is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// User ID of the workspace owner
        /// </summary>
        [MaxLength(100)]
        public string? OwnerId { get; set; }

        /// <summary>
        /// Members belonging to this workspace
        /// </summary>
        public virtual ICollection<Member> Members { get; set; } = new List<Member>();

        /// <summary>
        /// Todo items in this workspace.
        /// </summary>
        public virtual ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();

        /// <summary>
        /// Categories available in this workspace.
        /// </summary>
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

        /// <summary>
        /// Labels available in this workspace.
        /// </summary>
        public virtual ICollection<Label> Labels { get; set; } = new List<Label>();

        /// <summary>
        /// Analytics records for this workspace
        /// </summary>
        public virtual ICollection<WorkspaceAnalytics> Analytics { get; set; } = new List<WorkspaceAnalytics>();
    }
}
