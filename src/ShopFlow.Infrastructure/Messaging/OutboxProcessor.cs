using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopFlow.Application.Abstractions.Integrations;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Domain.Ordering.Events;
using ShopFlow.Infrastructure.Persistence;
using ShopFlow.Infrastructure.Persistence.Outbox;

namespace ShopFlow.Infrastructure.Messaging;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Falha ao processar outbox.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShopFlowDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var salesforce = scope.ServiceProvider.GetRequiredService<ISalesforceClient>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await DispatchAsync(message, eventBus, salesforce, blobStorage, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception exception)
            {
                message.Error = exception.Message;
                _logger.LogError(exception, "Outbox {MessageId} falhou.", message.Id);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task DispatchAsync(
        OutboxMessage message,
        IEventBus eventBus,
        ISalesforceClient salesforce,
        IBlobStorage blobStorage,
        CancellationToken cancellationToken)
    {
        var type = Type.GetType(message.Type);
        if (type is null)
        {
            throw new InvalidOperationException($"Tipo de evento {message.Type} não encontrado.");
        }

        var domainEvent = JsonSerializer.Deserialize(message.Payload, type)
            ?? throw new InvalidOperationException("Payload de outbox inválido.");

        if (domainEvent is OrderPlacedDomainEvent placed)
        {
            var integrationEvent = new OrderPlacedIntegrationEvent(
                placed.OrderId,
                placed.OrderNumber,
                placed.CustomerId,
                placed.TotalAmount,
                placed.Currency,
                placed.OccurredOnUtc);

            await eventBus.PublishAsync(integrationEvent, cancellationToken);
            await salesforce.SyncOrderAsync(
                new SalesforceOrderPayload(
                    placed.OrderNumber,
                    "sync@shopflow.dev",
                    placed.TotalAmount,
                    placed.Currency,
                    []),
                cancellationToken);
            await blobStorage.UploadJsonAsync("orders", $"{placed.OrderNumber}.json", integrationEvent, cancellationToken);
        }
        else
        {
            await eventBus.PublishAsync(domainEvent, cancellationToken);
        }
    }
}
