using Orders.Domain.Resources;

namespace Orders.Domain.Common;

/// <summary>
/// Thrown when a domain business rule is violated.
/// </summary>
/// <param name="code">
/// Stable error code, which is also the key of the message in <c>Resources/DomainErrors.resx</c>.
/// Use <c>nameof(DomainErrors.SomeKey)</c>.
/// </param>
public sealed class DomainException(string code)
    : Exception(DomainErrors.ResourceManager.GetString(code, DomainErrors.Culture) ?? code)
{
    public string Code { get; } = code;
}
