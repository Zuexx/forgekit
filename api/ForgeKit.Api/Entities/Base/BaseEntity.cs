using System;
using System.ComponentModel.DataAnnotations;

namespace ForgeKit.Api.Entities.Base
{
    /// <summary>
    /// Base entity class providing common audit fields for all entities
    /// </summary>
    public abstract class BaseEntity : IAuditableEntity, ISoftDelete
    {
        /// <summary>
        /// Unique identifier for the entity (32-character lowercase hex GUID)
        /// </summary>
        [Key]
        [MaxLength(32)]
        public string Id { get; set; }

        protected BaseEntity()
        {
            // Generate new GUID in format: Guid.NewGuid().ToString("N").ToLower()
            Id = Guid.NewGuid().ToString("N").ToLower();
        }

        /// <summary>
        /// Timestamp when the entity was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the entity was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// User identifier who created the entity
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User identifier who last updated the entity
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Version number for optimistic concurrency control
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Indicates if the entity has been soft deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Timestamp when the entity was soft deleted
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// User identifier who deleted the entity
        /// </summary>
        public string? DeletedBy { get; set; }
    }
}
