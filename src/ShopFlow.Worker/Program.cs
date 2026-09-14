using Serilog;
using ShopFlow.Application;
using ShopFlow.Infrastructure;
using ShopFlow.Infrastructure.Messaging;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddHostedService<OutboxProcessor>();

    var host = builder.Build();
    Log.Information("ShopFlow Worker Service (processamento da outbox) iniciado.");
    await host.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
