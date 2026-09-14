using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.BuildingBlocks.Results;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Ordering;

namespace ShopFlow.Application.Orders.Commands;

public sealed record CancelOrderCommand(Guid OrderId) : ICommand, ITransactionalRequest;

public sealed class CancelOrderCommandHandler : MediatR.IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IDateTimeProvider _clock;

    public CancelOrderCommandHandler(
        IOrderRepository orders,
        IProductRepository products,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _products = products;
        _clock = clock;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(Error.NotFound(nameof(Order), request.OrderId));
        }

        try
        {
            order.Cancel(_clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result.Failure(Error.Conflict(exception.Message));
        }

        var products = await _products.GetByIdsAsync(order.Items.Select(item => item.ProductId), cancellationToken);
        foreach (var item in order.Items)
        {
            products.Single(product => product.Id == item.ProductId).ReleaseStock(item.Quantity);
        }

        return Result.Success();
    }
}

public sealed record PayOrderCommand(Guid OrderId) : ICommand, ITransactionalRequest;

public sealed class PayOrderCommandHandler : MediatR.IRequestHandler<PayOrderCommand, Result>
{
    private readonly IOrderRepository _orders;
    private readonly IDateTimeProvider _clock;

    public PayOrderCommandHandler(IOrderRepository orders, IDateTimeProvider clock)
    {
        _orders = orders;
        _clock = clock;
    }

    public async Task<Result> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(Error.NotFound(nameof(Order), request.OrderId));
        }

        try
        {
            order.MarkAsPaid(_clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result.Failure(Error.Conflict(exception.Message));
        }

        return Result.Success();
    }
}
