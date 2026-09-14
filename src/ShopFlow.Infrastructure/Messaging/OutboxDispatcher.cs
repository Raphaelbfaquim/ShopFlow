using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShopFlow.Application.Abstractions.Integrations;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.Domain.Ordering.Events;
using ShopFlow.Infrastructure.Persistence.Outbox;

namespace ShopFlow.Infrastructure.Messaging;

public sealed class OutboxDispatcher
{
    public const int DefaultBatchSize = 20;

    public static readonly TimeSpan DefaultLockDuration = TimeSpan.FromSeconds(30);

    private readonly IOutboxStore _store;
    private readonly IEventBus _eventBus;
    private readonly ISalesforceClient _salesforce;
    private readonly IBlobStorage _blobStorage;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        IOutboxStore store,
        IEventBus eventBus,
        ISalesforceClient salesforce,
        IBlobStorage blobStorage,
        IDateTimeProvider clock,
        ILogger<OutboxDispatcher> logger)
    {
        _store = store;
        _eventBus = eventBus;
        _salesforce = salesforce;
        _blobStorage = blobStorage;
        _clock = clock;
        _logger = logger;
    }

    public async Task ProcessBatchAsync(string processorId, CancellationToken cancellationToken = default)
    {
        var claimed = await _store.ClaimPendingAsync(
            processorId,
            DefaultBatchSize,
            DefaultLockDuration,
            _clock.UtcNow,
            cancellationToken);

        foreach (var message in claimed)
        {
            try
            {
                await DispatchAsync(message, cancellationToken);
                message.MarkProcessed(_clock.UtcNow);
                await _store.SaveAsync(message, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.MarkFailed(exception.Message);
                await _store.SaveAsync(message, cancellationToken);
                _logger.LogError(exception, "Outbox {MessageId} falhou no processador {ProcessorId}.", message.Id, processorId);
            }
        }
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var type = Type.GetType(message.Type, throwOnError: false);
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

            if (!message.EventPublished)
            {
                await _eventBus.PublishAsync(integrationEvent, cancellationToken);
                message.MarkEventPublished();
                await _store.SaveAsync(message, cancellationToken);
            }

            if (!message.SalesforceSynced)
            {
                await _salesforce.SyncOrderAsync(
                    new SalesforceOrderPayload(
                        placed.OrderNumber,
                        "sync@shopflow.dev",
                        placed.TotalAmount,
                        placed.Currency,
                        []),
                    cancellationToken);
                message.MarkSalesforceSynced();
                await _store.SaveAsync(message, cancellationToken);
            }

            if (!message.BlobUploaded)
            {
                await _blobStorage.UploadJsonAsync("orders", $"{placed.OrderNumber}.json", integrationEvent, cancellationToken);
                message.MarkBlobUploaded();
                await _store.SaveAsync(message, cancellationToken);
            }

            return;
        }

        if (!message.EventPublished)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
            message.MarkEventPublished();
            await _store.SaveAsync(message, cancellationToken);
        }
    }
}
