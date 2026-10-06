using Orders.Domain.Common;

namespace Orders.Tests.Domain.Common;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithKnownCode_UsesMessageFromResources()
    {
        var exception = new DomainException("OrderClosed");

        Assert.Equal("OrderClosed", exception.Code);
        Assert.Equal("The order is closed and cannot be modified.", exception.Message);
    }

    [Fact]
    public void Constructor_WithUnknownCode_UsesCodeAsMessage()
    {
        var exception = new DomainException("SomethingUnknown");

        Assert.Equal("SomethingUnknown", exception.Code);
        Assert.Equal("SomethingUnknown", exception.Message);
    }
}
