using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ForgeKit.Api.Entities.Base;
using ForgeKit.Api.Interfaces;

namespace ForgeKit.Api.Data;

/// <summary>
/// Implementation of Unit of Work pattern for managing atomic transactions.
/// Provides transaction boundaries and automatic audit field management.
/// </summary>
public class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private readonly AppDbContext _dbContext = dbContext;
    private IDbContextTransaction? _transaction;

    public AppDbContext DbContext => _dbContext;

    /// <summary>
    /// Begin a database transaction. All subsequent operations will be atomic.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        return _transaction;
    }

    /// <summary>
    /// Commit the transaction. Saves all changes and commits the transaction.
    /// If SaveChanges fails, automatically rolls back.
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(ct);
            }
        }
        catch
        {
            await RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rollback the transaction. Reverts all changes made since BeginTransactionAsync.
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <summary>
    /// Save changes without audit field management.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Save changes and automatically populate CreatedBy/UpdatedBy fields.
    /// </summary>
    public async Task<int> SaveChangesAsync(string userId, CancellationToken ct = default)
    {
        SetAuditFields(userId);
        return await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Automatically set audit fields (CreatedBy, UpdatedBy) for auditable entities.
    /// </summary>
    private void SetAuditFields(string userId)
    {
        var entries = _dbContext.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                   (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (IAuditableEntity)entry.Entity;
            if (entry.State == EntityState.Added)
            {
                entity.CreatedBy = userId;
            }
            entity.UpdatedBy = userId;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext?.Dispose();
    }
}
