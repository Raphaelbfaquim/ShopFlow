using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ShopFlow.Application.Abstractions.Security;
using ShopFlow.Application.Catalog.Queries;
using ShopFlow.Application.Identity.Commands;
using ShopFlow.Application.Orders.Commands;
using ShopFlow.Infrastructure.Persistence;

namespace ShopFlow.Api.Tests;

public class ShopFlowApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Secret", "test-secret-key-must-be-at-least-32ch");
        builder.UseSetting("Jwt:Issuer", "ShopFlow");
        builder.UseSetting("Jwt:Audience", "ShopFlow");
        builder.UseSetting("Messaging:Provider", "InMemory");
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShopFlowDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DatabaseSeeder.SeedAsync(dbContext, hasher);
    }
}

public class CheckoutFlowTests : IClassFixture<ShopFlowApiFactory>, IAsyncLifetime
{
    private readonly ShopFlowApiFactory _factory;

    public CheckoutFlowTests(ShopFlowApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.SeedAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_Should_Return_Success()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Customer_Should_Login_List_Products_And_Place_Order()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("cliente@shopflow.dev", "Cliente@123"));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await login.Content.ReadFromJsonAsync<LoginResponse>();
        session.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var productsResponse = await client.GetAsync("/api/v1/products");
        productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await productsResponse.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products!.Should().NotBeEmpty();

        var orderResponse = await client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderCommand(
            [new PlaceOrderItem(products![0].Id, 1)],
            new PlaceOrderAddress("Rua Augusta", "100", "São Paulo", "SP", "01305-000", "BR"),
            "WELCOME10"));

        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Anonymous_User_Cannot_List_Products()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
