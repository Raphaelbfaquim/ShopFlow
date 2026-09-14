using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ShopFlow.Application.Abstractions.Integrations;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.Domain.Ordering.Events;
using ShopFlow.Infrastructure.Messaging;
using ShopFlow.Infrastructure.Persistence.Outbox;

namespace ShopFlow.Infrastructure.Tests.Messaging;

public class OutboxDispatcherTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 16, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Partial_Failure_Should_Keep_Published_Event_And_Retry_Remaining_Steps()
    {
        var message = CreateOrderPlacedMessage();
        var store = new ConcurrentOutboxStore(message);
        var eventBus = new Mock<IEventBus>();
        var salesforce = new Mock<ISalesforceClient>();
        var blob = new Mock<IBlobStorage>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(item => item.UtcNow).Returns(Now);

        salesforce
            .SetupSequence(item => item.SyncOrderAsync(It.IsAny<SalesforceOrderPayload>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Salesforce indisponível"))
            .Returns(Task.CompletedTask);

        var dispatcher = CreateDispatcher(store, eventBus, salesforce, blob, clock);

        await dispatcher.ProcessBatchAsync("worker-a");

        message.EventPublished.Should().BeTrue();
        message.SalesforceSynced.Should().BeFalse();
        message.BlobUploaded.Should().BeFalse();
        message.ProcessedOnUtc.Should().BeNull();
        message.Error.Should().Contain("Salesforce");
        eventBus.Verify(
            item => item.PublishAsync(It.IsAny<OrderPlacedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        await dispatcher.ProcessBatchAsync("worker-a");

        message.SalesforceSynced.Should().BeTrue();
        message.BlobUploaded.Should().BeTrue();
        message.ProcessedOnUtc.Should().Be(Now);
        message.Error.Should().BeNull();
        eventBus.Verify(
            item => item.PublishAsync(It.IsAny<OrderPlacedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        salesforce.Verify(
            item => item.SyncOrderAsync(It.IsAny<SalesforceOrderPayload>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        blob.Verify(
            item => item.UploadJsonAsync("orders", It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Concurrent_Workers_Should_Process_The_Same_Message_Once()
    {
        var message = CreateOrderPlacedMessage();
        var store = new ConcurrentOutboxStore(message);
        var eventBus = new Mock<IEventBus>();
        var publishStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        eventBus
            .Setup(item => item.PublishAsync(It.IsAny<OrderPlacedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(async (OrderPlacedIntegrationEvent _, CancellationToken _) =>
            {
                publishStarted.TrySetResult();
                await Task.Delay(100);
            });

        var salesforce = new Mock<ISalesforceClient>();
        salesforce
            .Setup(item => item.SyncOrderAsync(It.IsAny<SalesforceOrderPayload>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var blob = new Mock<IBlobStorage>();
        blob.Setup(item => item.UploadJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(item => item.UtcNow).Returns(Now);

        var workerA = CreateDispatcher(store, eventBus, salesforce, blob, clock);
        var workerB = CreateDispatcher(store, eventBus, salesforce, blob, clock);

        var first = workerA.ProcessBatchAsync("worker-a");
        await publishStarted.Task;
        await workerB.ProcessBatchAsync("worker-b");
        await first;

        eventBus.Verify(
            item => item.PublishAsync(It.IsAny<OrderPlacedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        message.ProcessedOnUtc.Should().Be(Now);
        message.Attempts.Should().Be(0);
    }

    [Fact]
    public async Task Locked_Message_Should_Not_Be_Claimed_By_Another_Worker()
    {
        var message = CreateOrderPlacedMessage();
        message.Claim("worker-a", Now.AddSeconds(30));
        var store = new ConcurrentOutboxStore(message);
        var eventBus = new Mock<IEventBus>();
        var dispatcher = CreateDispatcher(
            store,
            eventBus,
            new Mock<ISalesforceClient>(),
            new Mock<IBlobStorage>(),
            MockClock());

        await dispatcher.ProcessBatchAsync("worker-b");

        eventBus.Verify(
            item => item.PublishAsync(It.IsAny<OrderPlacedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        message.LockedBy.Should().Be("worker-a");
        message.ProcessedOnUtc.Should().BeNull();
    }

    private static OutboxDispatcher CreateDispatcher(
        IOutboxStore store,
        Mock<IEventBus> eventBus,
        Mock<ISalesforceClient> salesforce,
        Mock<IBlobStorage> blob,
        Mock<IDateTimeProvider> clock)
    {
        return new OutboxDispatcher(
            store,
            eventBus.Object,
            salesforce.Object,
            blob.Object,
            clock.Object,
            NullLogger<OutboxDispatcher>.Instance);
    }

    private static Mock<IDateTimeProvider> MockClock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(item => item.UtcNow).Returns(Now);
        return clock;
    }

    private static OutboxMessage CreateOrderPlacedMessage()
    {
        var domainEvent = new OrderPlacedDomainEvent(
            Guid.NewGuid(),
            "SF-1",
            Guid.NewGuid(),
            90m,
            "BRL");

        return new OutboxMessage
        {
            Id = domainEvent.EventId,
            Type = typeof(OrderPlacedDomainEvent).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };
    }
}
