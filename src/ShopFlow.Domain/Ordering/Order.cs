using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.BuildingBlocks.Guards;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Ordering.Events;
using ShopFlow.Domain.Ordering.Policies;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Ordering;

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    public OrderNumber Number { get; private set; } = null!;

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public Address ShippingAddress { get; private set; } = null!;

    public Money Subtotal { get; private set; } = null!;

    public Money Discount { get; private set; } = null!;

    public Money Total { get; private set; } = null!;

    public string DiscountPolicy { get; private set; } = "None";

    public DateTime PlacedAtUtc { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
    }

    private Order(
        Guid id,
        OrderNumber number,
        Guid customerId,
        Address shippingAddress,
        IReadOnlyCollection<OrderItem> items,
        IDiscountStrategy discountStrategy,
        DateTime placedAtUtc)
        : base(id)
    {
        Number = number;
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Placed;
        PlacedAtUtc = placedAtUtc;
        _items.AddRange(items);

        var currency = items.First().LineTotal.Currency;
        Subtotal = Money.Of(items.Sum(item => item.LineTotal.Amount), currency);
        Discount = discountStrategy.Calculate(Subtotal);
        Total = Subtotal.Subtract(Discount);
        DiscountPolicy = discountStrategy.Name;
    }

    public static Order Place(
        Guid customerId,
        Address shippingAddress,
        IReadOnlyCollection<OrderLine> lines,
        IDiscountStrategy discountStrategy,
        DateTime utcNow)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Order.Customer", "Cliente é obrigatório.");
        }

        Guard.AgainstNull(shippingAddress, nameof(shippingAddress));
        Guard.AgainstNull(discountStrategy, nameof(discountStrategy));

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException("Order.Empty", "Pedido precisa de ao menos um item.");
        }

        var items = lines.Select(line => line.ToOrderItem()).ToList();
        var order = new Order(
            Guid.NewGuid(),
            OrderNumber.New(utcNow),
            customerId,
            shippingAddress,
            items,
            discountStrategy,
            utcNow);

        if (order.Total.Amount <= 0)
        {
            throw new DomainException("Order.Total", "Total do pedido deve ser maior que zero.");
        }

        order.Raise(new OrderPlacedDomainEvent(
            order.Id,
            order.Number.Value,
            order.CustomerId,
            order.Total.Amount,
            order.Total.Currency));

        return order;
    }

    public void MarkAsPaid(DateTime utcNow)
    {
        EnsureStatus(OrderStatus.Placed, "somente pedidos colocados podem ser pagos.");
        Status = OrderStatus.Paid;
        PaidAtUtc = utcNow;
        Raise(new OrderPaidDomainEvent(Id, Number.Value, Total.Amount));
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            throw new DomainException("Order.Cancel", "Pedido já enviado não pode ser cancelado.");
        }

        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException("Order.Cancel", "Pedido já está cancelado.");
        }

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = utcNow;
        Raise(new OrderCancelledDomainEvent(Id, Number.Value));
    }

    public void MarkAsShipped()
    {
        EnsureStatus(OrderStatus.Paid, "somente pedidos pagos podem ser enviados.");
        Status = OrderStatus.Shipped;
    }

    private void EnsureStatus(OrderStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainException("Order.Status", $"Status atual {Status}: {message}");
        }
    }
}

public sealed record OrderLine(Guid ProductId, Sku Sku, string ProductName, Money UnitPrice, int Quantity)
{
    public OrderItem ToOrderItem() => new(ProductId, Sku, ProductName, UnitPrice, Quantity);
}
