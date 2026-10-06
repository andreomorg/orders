using Orders.Application.Common;

namespace Orders.Tests.Application.Common;

public class ApplicationErrorExceptionTests
{
    [Fact]
    public void Constructor_WithKnownCode_UsesMessageFromResources()
    {
        var exception = new NotFoundException("OrderNotFound");

        Assert.Equal("OrderNotFound", exception.Code);
        Assert.Equal("Order not found.", exception.Message);
    }

    [Fact]
    public void Constructor_WithUnknownCode_UsesCodeAsMessage()
    {
        var exception = new ValidationException("SomethingUnknown");

        Assert.Equal("SomethingUnknown", exception.Code);
        Assert.Equal("SomethingUnknown", exception.Message);
    }
}
