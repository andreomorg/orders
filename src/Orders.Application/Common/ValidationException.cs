namespace Orders.Application.Common;

/// <summary>
/// Thrown when an input parameter is invalid.
/// </summary>
public sealed class ValidationException(string code) : ApplicationErrorException(code);
