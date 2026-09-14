namespace ShopFlow.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "ShopFlow";

    public string Audience { get; set; } = "ShopFlow";

    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string Provider { get; set; } = "InMemory";

    public RabbitMqOptions RabbitMq { get; set; } = new();

    public SqsOptions Sqs { get; set; } = new();
}

public sealed class RabbitMqOptions
{
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";

    public string Exchange { get; set; } = "shopflow.events";
}

public sealed class SqsOptions
{
    public string QueueUrl { get; set; } = string.Empty;
}

public sealed class SalesforceOptions
{
    public const string SectionName = "Salesforce";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://example.my.salesforce.com/";

    public string Token { get; set; } = string.Empty;
}

public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string OrdersContainer { get; set; } = "orders";
}
