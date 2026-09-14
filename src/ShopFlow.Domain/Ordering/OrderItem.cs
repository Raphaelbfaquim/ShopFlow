using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.BuildingBlocks.Guards;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Ordering;

public sealed class OrderItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }

    public Sku Sku { get; private set; } = null!;

    public string ProductName { get; private set; } = string.Empty;

    public Money UnitPrice { get; private set; } = null!;

    public int Quantity { get; private set; }

    public Money LineTotal { get; private set; } = null!;

    private OrderItem()
    {
    }

    internal OrderItem(Guid productId, Sku sku, string productName, Money unitPrice, int quantity)
        : base(Guid.NewGuid())
    {
        ProductId = productId;
        Sku = sku;
        ProductName = Guard.AgainstNullOrWhiteSpace(productName, nameof(productName));
        UnitPrice = Guard.AgainstNull(unitPrice, nameof(unitPrice));
        Quantity = Guard.AgainstZeroOrNegative(quantity, nameof(quantity));
        LineTotal = unitPrice.Multiply(quantity);
    }
}
