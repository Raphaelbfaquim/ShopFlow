using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Infrastructure.Options;

namespace ShopFlow.Infrastructure.Messaging;

public sealed class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly string _exchange;

    public RabbitMqEventBus(IOptions<MessagingOptions> options, ILogger<RabbitMqEventBus> logger)
    {
        _logger = logger;
        var settings = options.Value;
        _exchange = settings.RabbitMq.Exchange;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(settings.RabbitMq.ConnectionString),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);
    }

    public string ProviderName => "RabbitMQ";

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var routingKey = typeof(TMessage).Name;
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(_exchange, routingKey, properties, body);
        _logger.LogInformation("Evento {EventType} publicado no RabbitMQ.", routingKey);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
