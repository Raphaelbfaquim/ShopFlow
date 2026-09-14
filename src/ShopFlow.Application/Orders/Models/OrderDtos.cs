namespace ShopFlow.Application.Orders.Models;

public sealed record OrderDetailsDto(
    Guid Id,
    string Number,
    Guid CustomerId,
    string Status,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    string Currency,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderDashboardDto(int TotalOrders, int Placed, int Paid, int Cancelled, decimal Revenue);
