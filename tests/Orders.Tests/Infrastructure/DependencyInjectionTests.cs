using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;
using Orders.Infrastructure;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Persistence.Repositories;

namespace Orders.Tests.Infrastructure;

public class DependencyInjectionTests
{
    [Theory]
    [InlineData(typeof(IOrderRepository), typeof(OrderRepository))]
    [InlineData(typeof(IProductRepository), typeof(ProductRepository))]
    public void AddInfrastructure_RegistersRepositoryAsScoped(Type serviceType, Type implementationType)
    {
        var services = new ServiceCollection();

        services.AddInfrastructure();

        var descriptor = Assert.Single(services, registration => registration.ServiceType == serviceType);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public async Task AddInfrastructure_UnitOfWorkIsTheSameContextOfTheRequest()
    {
        await using var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Assert.Same(context, unitOfWork);
    }

    [Fact]
    public async Task InitializeDatabaseAsync_CreatesDatabaseWithProductCatalog()
    {
        await using var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        await provider.InitializeDatabaseAsync();

        await using var scope = provider.CreateAsyncScope();
        var products = await scope.ServiceProvider.GetRequiredService<IProductRepository>().ListAsync();
        Assert.Equal(6, products.Count);
    }
}
