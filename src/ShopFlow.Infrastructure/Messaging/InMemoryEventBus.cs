using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ShopFlow.Application.Abstractions.Messaging;

namespace ShopFlow.Infrastructure.Messaging;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentQueue<object> _messages = new();
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "InMemory";

    public IReadOnlyCollection<object> Snapshot => _messages.ToArray();

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _messages.Enqueue(message);
        _logger.LogInformation("Evento {EventType} publicado no EventBus em memória.", typeof(TMessage).Name);
        return Task.CompletedTask;
    }
}
