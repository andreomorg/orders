using Orders.Application.Resources;

namespace Orders.Application.Common;

/// <summary>
/// Pagination limits shared by every paged listing.
/// </summary>
public static class Pagination
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    /// <summary>
    /// Highest page whose offset, <c>(page - 1) * pageSize</c>, still fits in an <see cref="int"/>.
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public static void Validate(int page, int pageSize)
    {
        if (page is < 1 or > MaxPage)
            throw new ValidationException(nameof(ApplicationErrors.InvalidPage));

        if (pageSize is < 1 or > MaxPageSize)
            throw new ValidationException(nameof(ApplicationErrors.InvalidPageSize));
    }
}
