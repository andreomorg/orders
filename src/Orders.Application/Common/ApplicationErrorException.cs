using Orders.Application.Resources;

namespace Orders.Application.Common;

/// <summary>
/// Base for errors raised by the application layer.
/// </summary>
/// <param name="code">
/// Stable error code, which is also the key of the message in <c>Resources/ApplicationErrors.resx</c>.
/// Use <c>nameof(ApplicationErrors.SomeKey)</c>.
/// </param>
public abstract class ApplicationErrorException(string code)
    : Exception(ApplicationErrors.ResourceManager.GetString(code, ApplicationErrors.Culture) ?? code)
{
    public string Code { get; } = code;
}
