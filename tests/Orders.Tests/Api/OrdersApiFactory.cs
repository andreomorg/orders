using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Application.Orders;
using Orders.Infrastructure.Persistence;

namespace Orders.Tests.Api;

/// <summary>
/// Runs the real API in memory, with its own InMemory database, so a test never sees another test's data.
/// Optionally replaces services, to force situations that requests alone cannot produce reliably.
/// </summary>
public sealed class OrdersApiFactory(Action<IServiceCollection>? replaceServices = null)
    : WebApplicationFactory<Program>
{
    /// <summary>
    /// Same JSON format as the API: camelCase and enums as text.
    /// </summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _databaseName = $"orders-api-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrdersDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<OrdersDbContext>>();
            services.AddDbContext<OrdersDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            replaceServices?.Invoke(services);
        });
}

/// <summary>
/// Shortcuts for the requests most tests need before the one being tested.
/// </summary>
internal static class OrdersApiClientExtensions
{
    public static readonly Guid NotebookId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid MouseId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public static async Task<OrderDto> CreateOrderAsync(this HttpClient client)
    {
        var response = await client.PostAsync("/api/orders", content: null);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<OrderDto>();
    }

    public static async Task<OrderDto> AddItemAsync(this HttpClient client, Guid orderId, Guid productId, int quantity)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/items", new { productId, quantity });
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<OrderDto>();
    }

    public static async Task<OrderDto> CloseOrderAsync(this HttpClient client, Guid orderId)
    {
        var response = await client.PostAsync($"/api/orders/{orderId}/close", content: null);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<OrderDto>();
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(OrdersApiFactory.Json))!;
}
