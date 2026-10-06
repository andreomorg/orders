using Orders.Application.Common;

namespace Orders.Tests.Application.Common;

public class PaginationTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 10)]
    [InlineData(5, 50)]
    public void Validate_WithValidValues_DoesNotThrow(int page, int pageSize)
    {
        var exception = Record.Exception(() => Pagination.Validate(page, pageSize));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithPageLowerThanOne_ThrowsInvalidPage(int page)
    {
        var exception = Assert.Throws<ValidationException>(() => Pagination.Validate(page, 10));

        Assert.Equal("InvalidPage", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_WithPageSizeOutOfRange_ThrowsInvalidPageSize(int pageSize)
    {
        var exception = Assert.Throws<ValidationException>(() => Pagination.Validate(1, pageSize));

        Assert.Equal("InvalidPageSize", exception.Code);
    }
}
