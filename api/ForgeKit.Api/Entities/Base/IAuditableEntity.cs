using System;

namespace ForgeKit.Api.Entities.Base
{
    /// <summary>
    /// Interface for entities that require audit tracking
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>
        /// Timestamp when the entity was created
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the entity was last updated
        /// </summary>
        DateTime UpdatedAt { get; set; }

        /// <summary>
        /// User identifier who created the entity
        /// </summary>
        string? CreatedBy { get; set; }

        /// <summary>
        /// User identifier who last updated the entity
        /// </summary>
        string? UpdatedBy { get; set; }

        /// <summary>
        /// Version number for optimistic concurrency control
        /// </summary>
        int Version { get; set; }
    }
}
