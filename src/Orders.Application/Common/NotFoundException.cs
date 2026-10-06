namespace Orders.Application.Common;

/// <summary>
/// Thrown when a requested resource does not exist.
/// </summary>
public sealed class NotFoundException(string code) : ApplicationErrorException(code);
