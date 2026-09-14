using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Orders.Models;

namespace ShopFlow.Infrastructure.Persistence.Read;

public sealed class DapperOrderReadRepository : IOrderReadRepository
{
    private readonly string _connectionString;

    public DapperOrderReadRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ShopFlow")
            ?? throw new InvalidOperationException("Connection string 'ShopFlow' não configurada.");
    }

    public async Task<OrderDetailsDto?> GetDetailsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                o.Id,
                o.Number,
                o.CustomerId,
                o.Status,
                o.Subtotal,
                o.Discount,
                o.Total,
                o.TotalCurrency AS Currency
            FROM Orders o
            WHERE o.Id = @orderId;

            SELECT
                i.Sku,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal
            FROM OrderItems i
            WHERE i.OrderId = @orderId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { orderId }, cancellationToken: cancellationToken));
        var header = await multi.ReadFirstOrDefaultAsync<OrderHeaderRow>();
        if (header is null)
        {
            return null;
        }

        var items = (await multi.ReadAsync<OrderItemDto>()).ToList();
        return new OrderDetailsDto(
            header.Id,
            header.Number,
            header.CustomerId,
            header.Status,
            header.Subtotal,
            header.Discount,
            header.Total,
            header.Currency,
            items);
    }

    public async Task<OrderDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                COUNT(*) AS TotalOrders,
                ISNULL(SUM(CASE WHEN Status = 'Placed' THEN 1 ELSE 0 END), 0) AS Placed,
                ISNULL(SUM(CASE WHEN Status = 'Paid' THEN 1 ELSE 0 END), 0) AS Paid,
                ISNULL(SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END), 0) AS Cancelled,
                ISNULL(SUM(CASE WHEN Status = 'Paid' THEN Total ELSE 0 END), 0) AS Revenue
            FROM Orders;
            """;

        await using var connection = new SqlConnection(_connectionString);
        var row = await connection.QuerySingleAsync<OrderDashboardDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return row with
        {
            Revenue = row.Revenue
        };
    }

    private sealed class OrderHeaderRow
    {
        public Guid Id { get; init; }

        public string Number { get; init; } = string.Empty;

        public Guid CustomerId { get; init; }

        public string Status { get; init; } = string.Empty;

        public decimal Subtotal { get; init; }

        public decimal Discount { get; init; }

        public decimal Total { get; init; }

        public string Currency { get; init; } = "BRL";
    }
}
