using System;

namespace ForgeKit.Api.Entities.Base
{
    /// <summary>
    /// Interface for entities that support soft delete
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>
        /// Indicates if the entity has been soft deleted
        /// </summary>
        bool IsDeleted { get; set; }

        /// <summary>
        /// Timestamp when the entity was soft deleted
        /// </summary>
        DateTime? DeletedAt { get; set; }

        /// <summary>
        /// User identifier who deleted the entity
        /// </summary>
        string? DeletedBy { get; set; }
    }
}
