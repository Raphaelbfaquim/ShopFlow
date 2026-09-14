using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.BuildingBlocks.Specifications;
using ShopFlow.Domain.Ordering;

namespace ShopFlow.Application.Abstractions.Persistence;

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<Order?> GetByNumberAsync(OrderNumber number, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListAsync(ISpecification<Order> specification, CancellationToken cancellationToken = default);
}
