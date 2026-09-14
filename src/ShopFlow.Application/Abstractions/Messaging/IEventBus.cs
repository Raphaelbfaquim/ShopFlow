namespace ShopFlow.Application.Abstractions.Messaging;

public interface IEventBus
{
    string ProviderName { get; }

    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
