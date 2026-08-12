using Anvil.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Anvil.Interfaces;

/// <summary>
/// Unit of Work pattern for managing atomic transactions across multiple entities.
/// Provides transaction boundaries and audit field management.
/// </summary>
/// <remarks>
/// Generic over the context so this contract can live in the shared layer while each
/// product keeps typed access to its own DbSets.
/// </summary>
public interface IUnitOfWork<out TContext> : IDisposable
    where TContext : PlatformDbContext
{
    /// <summary>
    /// Direct access to DbContext for queries and entity operations.
    /// Application Service should use this for both queries and adds/updates.
    /// </summary>
    TContext DbContext { get; }

    /// <summary>
    /// Begin a database transaction. All subsequent SaveChanges will be atomic.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Commit the transaction. Saves all changes atomically, then commits transaction.
    /// Throws if SaveChanges fails (automatic rollback occurs).
    /// </summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Rollback the transaction. Reverts all changes made since BeginTransactionAsync.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Save changes without populating audit fields.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Save changes and automatically set CreatedBy/UpdatedBy fields for auditable entities.
    /// </summary>
    Task<int> SaveChangesAsync(string userId, CancellationToken ct = default);
}
