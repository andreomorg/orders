namespace Orders.Domain.Common;

/// <summary>
/// Commits the changes tracked by the repositories in a single operation.
/// </summary>
public interface IUnitOfWork
{
    /// <exception cref="ConcurrencyException">Another request saved the same data first.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
