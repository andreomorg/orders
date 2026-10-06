using Microsoft.Extensions.DependencyInjection;
using Orders.Application;
using Orders.Application.Orders;
using Orders.Application.Products;

namespace Orders.Tests.Application;

public class DependencyInjectionTests
{
    [Theory]
    [InlineData(typeof(IOrderService), typeof(OrderService))]
    [InlineData(typeof(IProductService), typeof(ProductService))]
    public void AddApplication_RegistersServiceAsScoped(Type serviceType, Type implementationType)
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services, registration => registration.ServiceType == serviceType);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
