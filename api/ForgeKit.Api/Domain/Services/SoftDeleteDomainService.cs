using ForgeKit.Api.Entities.Base;

namespace ForgeKit.Api.Domain.Services;

/// <summary>
/// Domain Service for handling soft delete operations with audit context.
/// Provides consistent methods for marking entities as deleted and restoring them.
/// </summary>
public class SoftDeleteDomainService
{
    /// <summary>
    /// Marks an entity as soft-deleted with audit information.
    /// </summary>
    /// <typeparam name="T">Entity type that implements ISoftDelete</typeparam>
    /// <param name="entity">The entity to mark as deleted</param>
    /// <param name="deletedBy">User ID who performed the deletion (from IAuditContext.UserId)</param>
    /// <param name="deletedAt">Timestamp of deletion (optional, defaults to DateTime.UtcNow)</param>
    /// <remarks>
    /// This method updates:
    /// - IsDeleted: Set to true
    /// - DeletedAt: Set to the provided timestamp (or current UTC time)
    /// - DeletedBy: Set to the provided userId
    /// 
    /// Note: The entity must be tracked by DbContext. Call this method before SaveChangesAsync().
    /// </remarks>
    public void MarkAsDeleted<T>(T entity, string deletedBy, DateTime? deletedAt = null) where T : BaseEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("DeletedBy cannot be null or empty", nameof(deletedBy));

        entity.IsDeleted = true;
        entity.DeletedAt = deletedAt ?? DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a soft-deleted entity back to active state.
    /// </summary>
    /// <typeparam name="T">Entity type that implements ISoftDelete</typeparam>
    /// <param name="entity">The entity to restore</param>
    /// <param name="restoredBy">User ID who performed the restoration (from IAuditContext.UserId)</param>
    /// <remarks>
    /// This method updates:
    /// - IsDeleted: Set to false
    /// - DeletedAt: Set to null
    /// - DeletedBy: Set to null
    /// - UpdatedBy: Set to the provided userId
    /// - UpdatedAt: Set to DateTime.UtcNow
    /// 
    /// Note: The entity must be tracked by DbContext. Call this method before SaveChangesAsync().
    /// </remarks>
    public void Restore<T>(T entity, string restoredBy) where T : BaseEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(restoredBy))
            throw new ArgumentException("RestoredBy cannot be null or empty", nameof(restoredBy));

        if (!CanRestore(entity))
            throw new InvalidOperationException("Soft-deleted entities can only be restored within the restore grace period.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.UpdatedBy = restoredBy;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if an entity can be restored based on business rules.
    /// </summary>
    /// <typeparam name="T">Entity type that implements ISoftDelete</typeparam>
    /// <param name="entity">The entity to check</param>
    /// <param name="restoreDaysLimit">Maximum number of days after deletion to allow restoration (default: 30)</param>
    /// <returns>True if entity can be restored, false otherwise</returns>
    /// <remarks>
    /// Current business rule: Only restore if deleted within the specified number of days ago.
    /// This can be extended with additional business rules as needed.
    /// </remarks>
    public bool CanRestore<T>(T entity, int restoreDaysLimit = 30) where T : BaseEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return entity.IsDeleted &&
               entity.DeletedAt.HasValue &&
               (DateTime.UtcNow - entity.DeletedAt.Value).TotalDays <= restoreDaysLimit;
    }
}
