using Microsoft.EntityFrameworkCore;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.BuildingBlocks.Specifications;
using ShopFlow.Domain.Ordering;

namespace ShopFlow.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly ShopFlowDbContext _dbContext;

    public OrderRepository(ShopFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public Task<Order?> GetByNumberAsync(OrderNumber number, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Number == number, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListAsync(
        ISpecification<Order> specification,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(order => order.Items)
            .Where(specification.ToExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order aggregate, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(aggregate, cancellationToken);
    }
}
