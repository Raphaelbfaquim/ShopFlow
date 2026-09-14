using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Infrastructure.Options;

namespace ShopFlow.Infrastructure.Messaging;

public sealed class SqsEventBus : IEventBus
{
    private readonly IAmazonSQS _client;
    private readonly string _queueUrl;
    private readonly ILogger<SqsEventBus> _logger;

    public SqsEventBus(IOptions<MessagingOptions> options, ILogger<SqsEventBus> logger)
        : this(options, logger, new AmazonSQSClient())
    {
    }

    public SqsEventBus(IOptions<MessagingOptions> options, ILogger<SqsEventBus> logger, IAmazonSQS client)
    {
        _logger = logger;
        _client = client;
        _queueUrl = options.Value.Sqs.QueueUrl;
    }

    public string ProviderName => "Sqs";

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = JsonSerializer.Serialize(message),
            MessageAttributes =
            {
                ["EventType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = typeof(TMessage).Name
                }
            }
        };

        await _client.SendMessageAsync(request, cancellationToken);
        _logger.LogInformation("Evento {EventType} publicado no AWS SQS.", typeof(TMessage).Name);
    }
}
