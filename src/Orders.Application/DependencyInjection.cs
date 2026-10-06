using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Orders;
using Orders.Application.Products;

namespace Orders.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application services (use cases).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
