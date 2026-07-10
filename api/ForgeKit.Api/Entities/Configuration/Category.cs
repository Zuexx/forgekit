using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Entities.Core;
using ForgeKit.Api.Entities.Todos;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Configuration
{
    /// <summary>
    /// Represents a hierarchical category used to classify todo items
    /// </summary>
    [Table("Categories")]
    [Index(nameof(CategoryCode), IsUnique = true)]
    [Index(nameof(ParentCategoryId))]
    [Index(nameof(WorkspaceId))]
    public class Category : BaseEntity
    {
        /// <summary>
        /// Optional workspace ID; null indicates a global category available to all workspaces
        /// </summary>
        [MaxLength(32)]
        public string? WorkspaceId { get; set; }

        /// <summary>
        /// Unique code identifying the category
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string CategoryCode { get; set; }

        /// <summary>
        /// Display name of the category
        /// </summary>
        [Required]
        [MaxLength(200)]
        public required string CategoryName { get; set; }

        /// <summary>
        /// Hex color code for the category, e.g. "#3B82F6"
        /// </summary>
        [MaxLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// ID of the parent category for hierarchical nesting; null if root category
        /// </summary>
        [MaxLength(32)]
        public string? ParentCategoryId { get; set; }

        /// <summary>
        /// Ordering position used when displaying categories
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indicates whether the category is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional description of the category
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Workspace that owns this category; null for global categories.
        /// </summary>
        [ForeignKey(nameof(WorkspaceId))]
        public virtual Workspace? Workspace { get; set; }

        /// <summary>
        /// Parent category in the hierarchy
        /// </summary>
        [ForeignKey(nameof(ParentCategoryId))]
        public virtual Category? ParentCategory { get; set; }

        /// <summary>
        /// Child categories nested under this category
        /// </summary>
        public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();

        /// <summary>
        /// Labels associated with this category
        /// </summary>
        public virtual ICollection<CategoryLabel> CategoryLabels { get; set; } = new List<CategoryLabel>();

        /// <summary>
        /// Todo items classified by this category.
        /// </summary>
        public virtual ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
    }
}
