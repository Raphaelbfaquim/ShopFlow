using ShopFlow.Application.Orders.Models;

namespace ShopFlow.Application.Abstractions.Persistence;

public interface IOrderReadRepository
{
    Task<OrderDetailsDto?> GetDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<OrderDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
