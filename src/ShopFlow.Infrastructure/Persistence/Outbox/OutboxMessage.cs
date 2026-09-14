namespace ShopFlow.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredOnUtc { get; set; }

    public DateTime? ProcessedOnUtc { get; set; }

    public string? Error { get; set; }

    public int Attempts { get; set; }

    public string? LockedBy { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public bool EventPublished { get; set; }

    public bool SalesforceSynced { get; set; }

    public bool BlobUploaded { get; set; }

    public int Version { get; set; }

    public bool IsProcessed => ProcessedOnUtc is not null;

    public bool CanClaim(DateTime utcNow) =>
        ProcessedOnUtc is null && (LockedUntilUtc is null || LockedUntilUtc < utcNow);

    public void Claim(string processorId, DateTime lockedUntilUtc)
    {
        LockedBy = processorId;
        LockedUntilUtc = lockedUntilUtc;
        Version++;
    }

    public void MarkEventPublished()
    {
        EventPublished = true;
        Version++;
    }

    public void MarkSalesforceSynced()
    {
        SalesforceSynced = true;
        Version++;
    }

    public void MarkBlobUploaded()
    {
        BlobUploaded = true;
        Version++;
    }

    public void MarkProcessed(DateTime utcNow)
    {
        ProcessedOnUtc = utcNow;
        Error = null;
        LockedBy = null;
        LockedUntilUtc = null;
        Version++;
    }

    public void MarkFailed(string error)
    {
        Error = error;
        Attempts++;
        LockedBy = null;
        LockedUntilUtc = null;
        Version++;
    }
}
