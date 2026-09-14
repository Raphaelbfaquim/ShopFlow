using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Orders.Models;
using ShopFlow.BuildingBlocks.Results;
using ShopFlow.Domain.Ordering;

namespace ShopFlow.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailsDto>;

public sealed class GetOrderByIdQueryHandler : MediatR.IRequestHandler<GetOrderByIdQuery, Result<OrderDetailsDto>>
{
    private readonly IOrderReadRepository _readRepository;

    public GetOrderByIdQueryHandler(IOrderReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<OrderDetailsDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _readRepository.GetDetailsAsync(request.OrderId, cancellationToken);
        return order is null
            ? Result.Failure<OrderDetailsDto>(Error.NotFound(nameof(Order), request.OrderId))
            : Result.Success(order);
    }
}

public sealed record GetOrderDashboardQuery : IQuery<OrderDashboardDto>;

public sealed class GetOrderDashboardQueryHandler : MediatR.IRequestHandler<GetOrderDashboardQuery, Result<OrderDashboardDto>>
{
    private readonly IOrderReadRepository _readRepository;

    public GetOrderDashboardQueryHandler(IOrderReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<OrderDashboardDto>> Handle(GetOrderDashboardQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await _readRepository.GetDashboardAsync(cancellationToken);
        return Result.Success(dashboard);
    }
}
