using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShopFlow.Application.Abstractions.Caching;
using ShopFlow.Application.Abstractions.Integrations;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Abstractions.Security;
using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.Infrastructure.Caching;
using ShopFlow.Infrastructure.Integrations;
using ShopFlow.Infrastructure.Messaging;
using ShopFlow.Infrastructure.Options;
using ShopFlow.Infrastructure.Persistence;
using ShopFlow.Infrastructure.Persistence.Read;
using ShopFlow.Infrastructure.Persistence.Repositories;
using ShopFlow.Infrastructure.Security;
using ShopFlow.Infrastructure.Time;

namespace ShopFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));
        services.Configure<SalesforceOptions>(configuration.GetSection(SalesforceOptions.SectionName));
        services.Configure<AzureStorageOptions>(configuration.GetSection(AzureStorageOptions.SectionName));

        var connectionString = configuration.GetConnectionString("ShopFlow")
            ?? "Server=localhost,1433;Database=ShopFlow;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False;";

        if (environment.IsEnvironment("Testing"))
        {
            services.AddDbContext<ShopFlowDbContext>(options => options.UseInMemoryDatabase("ShopFlowTests"));
            services.AddScoped<IOrderReadRepository, EfOrderReadRepository>();
        }
        else
        {
            services.AddDbContext<ShopFlowDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IOrderReadRepository, DapperOrderReadRepository>();
        }

        services.AddHealthChecks().AddDbContextCheck<ShopFlowDbContext>();

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ShopFlowDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        AddCache(services, configuration);
        AddMessaging(services, configuration);
        AddSalesforce(services, configuration);
        AddStorage(services, configuration);

        return services;
    }

    private static void AddCache(IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
            return;
        }

        services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        services.AddSingleton<ICacheService, RedisCacheService>();
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>($"{MessagingOptions.SectionName}:Provider") ?? "InMemory";
        switch (provider.ToLowerInvariant())
        {
            case "rabbitmq":
                services.AddSingleton<IEventBus, RabbitMqEventBus>();
                break;
            case "sqs":
                services.AddSingleton<IEventBus, SqsEventBus>();
                break;
            default:
                services.AddSingleton<IEventBus, InMemoryEventBus>();
                break;
        }
    }

    private static void AddSalesforce(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(SalesforceOptions.SectionName).Get<SalesforceOptions>() ?? new SalesforceOptions();
        if (!options.Enabled)
        {
            services.AddSingleton<ISalesforceClient, LoggingSalesforceAdapter>();
            return;
        }

        services.AddHttpClient<ISalesforceClient, SalesforceHttpAdapter>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                if (!string.IsNullOrWhiteSpace(options.Token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Token);
                }
            })
            .AddStandardResilienceHandler();
    }

    private static void AddStorage(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AzureStorageOptions.SectionName).Get<AzureStorageOptions>() ?? new AzureStorageOptions();
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            services.AddSingleton<IBlobStorage, LocalBlobStorage>();
            return;
        }

        services.AddSingleton(_ => new BlobServiceClient(options.ConnectionString));
        services.AddSingleton<IBlobStorage, AzureBlobStorage>();
    }
}
