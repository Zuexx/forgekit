using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Todos;

/// <summary>
/// Immutable status-change record for the sample TodoItem entity.
/// </summary>
[Table("TodoStatusHistory")]
[Index(nameof(TodoItemId), nameof(Timestamp))]
public class TodoStatusHistory : BaseEntity
{
    /// <summary>
    /// Todo whose status changed.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public required string TodoItemId { get; set; }

    /// <summary>
    /// Status after the change.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Status { get; set; }

    /// <summary>
    /// UTC timestamp when the status changed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User ID that made the change.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string ChangedBy { get; set; }

    /// <summary>
    /// Optional change note.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Todo navigation.
    /// </summary>
    [ForeignKey(nameof(TodoItemId))]
    public virtual TodoItem? TodoItem { get; set; }
}
