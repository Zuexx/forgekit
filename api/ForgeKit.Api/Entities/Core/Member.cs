using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Entities.Todos;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Core
{
    /// <summary>
    /// Represents a user's membership within a workspace
    /// </summary>
    [Table("Members")]
    [Index(nameof(WorkspaceId), nameof(UserId), IsUnique = true)]
    [Index(nameof(UserId))]
    public class Member : BaseEntity
    {
        /// <summary>
        /// ID of the workspace this member belongs to
        /// </summary>
        [Required]
        [MaxLength(32)]
        public required string WorkspaceId { get; set; }

        /// <summary>
        /// Auth user ID of the member
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string UserId { get; set; }

        /// <summary>
        /// Role of the member in the workspace, e.g. "Owner", "Admin", "Member"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string Role { get; set; }

        /// <summary>
        /// Timestamp when the user joined the workspace
        /// </summary>
        public DateTime JoinedAt { get; set; }

        /// <summary>
        /// The workspace this member belongs to
        /// </summary>
        [ForeignKey(nameof(WorkspaceId))]
        public virtual Workspace? Workspace { get; set; }

        /// <summary>
        /// Todo items assigned to this member.
        /// </summary>
        public virtual ICollection<TodoItem> AssignedTodoItems { get; set; } = new List<TodoItem>();
    }
}
