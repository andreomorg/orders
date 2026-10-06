using Orders.Domain.Resources;

namespace Orders.Domain.Common;

/// <summary>
/// Thrown by <see cref="IUnitOfWork.SaveChangesAsync"/> when another request saved the same data first.
/// The client should reload the data and try again.
/// </summary>
public sealed class ConcurrencyException(Exception? innerException = null)
    : Exception(DomainErrors.ConcurrencyConflict, innerException)
{
    public string Code => nameof(DomainErrors.ConcurrencyConflict);
}
