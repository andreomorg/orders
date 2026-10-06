using Orders.Domain.Common;

namespace Orders.Tests.Domain.Common;

public class ConcurrencyExceptionTests
{
    [Fact]
    public void Constructor_UsesCodeAndMessageFromResources()
    {
        var innerException = new InvalidOperationException();

        var exception = new ConcurrencyException(innerException);

        Assert.Equal("ConcurrencyConflict", exception.Code);
        Assert.Equal("The data was changed by another request. Reload it and try again.", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }
}
