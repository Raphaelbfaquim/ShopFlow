using Microsoft.EntityFrameworkCore;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Orders.Models;
using ShopFlow.Domain.Ordering;

namespace ShopFlow.Infrastructure.Persistence.Read;

public sealed class EfOrderReadRepository : IOrderReadRepository
{
    private readonly ShopFlowDbContext _dbContext;

    public EfOrderReadRepository(ShopFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderDetailsDto?> GetDetailsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderDetailsDto(
            order.Id,
            order.Number.Value,
            order.CustomerId,
            order.Status.ToString(),
            order.Subtotal.Amount,
            order.Discount.Amount,
            order.Total.Amount,
            order.Total.Currency,
            order.Items.Select(item => new OrderItemDto(
                item.Sku.Value,
                item.ProductName,
                item.Quantity,
                item.UnitPrice.Amount,
                item.LineTotal.Amount)).ToList());
    }

    public async Task<OrderDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Orders.AsNoTracking().ToListAsync(cancellationToken);
        return new OrderDashboardDto(
            orders.Count,
            orders.Count(order => order.Status == OrderStatus.Placed),
            orders.Count(order => order.Status == OrderStatus.Paid),
            orders.Count(order => order.Status == OrderStatus.Cancelled),
            orders.Where(order => order.Status == OrderStatus.Paid).Sum(order => order.Total.Amount));
    }
}
