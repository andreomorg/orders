namespace Orders.Domain.Common;

/// <summary>
/// Commits the changes tracked by the repositories in a single operation.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
