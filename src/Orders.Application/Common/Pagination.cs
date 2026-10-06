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

    public static void Validate(int page, int pageSize)
    {
        if (page < 1)
            throw new ValidationException(nameof(ApplicationErrors.InvalidPage));

        if (pageSize is < 1 or > MaxPageSize)
            throw new ValidationException(nameof(ApplicationErrors.InvalidPageSize));
    }
}
