using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Entities.Configuration;
using ForgeKit.Api.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Todos;

/// <summary>
/// Sample work-item entity used to demonstrate starter-kit persistence conventions.
/// </summary>
[Table("TodoItems")]
[Index(nameof(WorkspaceId), nameof(CurrentStatus), nameof(IsDeleted))]
[Index(nameof(AssignedToMemberId))]
[Index(nameof(CategoryId))]
[Index(nameof(DueDate))]
public class TodoItem : BaseEntity
{
    /// <summary>
    /// Workspace that owns this todo.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public required string WorkspaceId { get; set; }

    /// <summary>
    /// Optional member assigned to complete the todo.
    /// </summary>
    [MaxLength(32)]
    public string? AssignedToMemberId { get; set; }

    /// <summary>
    /// Optional category used to classify the todo.
    /// </summary>
    [MaxLength(32)]
    public string? CategoryId { get; set; }

    /// <summary>
    /// Short human-readable title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    /// <summary>
    /// Optional detailed description.
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Priority label such as Low, Medium, or High.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public required string Priority { get; set; }

    /// <summary>
    /// Current workflow status such as Todo, InProgress, Done, or Cancelled.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string CurrentStatus { get; set; }

    /// <summary>
    /// Optional target completion date.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Timestamp when the todo was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Optional JSON metadata for sample extensions.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Workspace navigation.
    /// </summary>
    [ForeignKey(nameof(WorkspaceId))]
    public virtual Workspace? Workspace { get; set; }

    /// <summary>
    /// Assigned member navigation.
    /// </summary>
    [ForeignKey(nameof(AssignedToMemberId))]
    public virtual Member? AssignedTo { get; set; }

    /// <summary>
    /// Category navigation.
    /// </summary>
    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }

    /// <summary>
    /// Status history entries for this todo.
    /// </summary>
    public virtual ICollection<TodoStatusHistory> StatusHistory { get; set; } = new List<TodoStatusHistory>();
}
