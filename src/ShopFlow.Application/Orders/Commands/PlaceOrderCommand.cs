using FluentValidation;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Abstractions.Security;
using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.BuildingBlocks.Results;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Ordering;
using ShopFlow.Domain.Ordering.Policies;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Application.Orders.Commands;

public sealed record PlaceOrderItem(Guid ProductId, int Quantity);

public sealed record PlaceOrderAddress(string Street, string Number, string City, string State, string ZipCode, string Country);

public sealed record PlaceOrderCommand(
    IReadOnlyList<PlaceOrderItem> Items,
    PlaceOrderAddress ShippingAddress,
    string? Coupon)
    : ICommand<Guid>, ITransactionalRequest;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(command => command.Items).NotEmpty();
        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
        RuleFor(command => command.ShippingAddress).NotNull();
        RuleFor(command => command.ShippingAddress.Street).NotEmpty();
        RuleFor(command => command.ShippingAddress.City).NotEmpty();
        RuleFor(command => command.ShippingAddress.ZipCode).NotEmpty();
    }
}

public sealed class PlaceOrderCommandHandler : MediatR.IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    private readonly IProductRepository _products;
    private readonly IOrderRepository _orders;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public PlaceOrderCommandHandler(
        IProductRepository products,
        IOrderRepository orders,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _products = products;
        _orders = orders;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result.Failure<Guid>(Error.Unauthorized());
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            return Result.Failure<Guid>(Error.NotFound(nameof(Product), "um ou mais produtos"));
        }

        var productMap = products.ToDictionary(product => product.Id);
        var lines = new List<OrderLine>();

        foreach (var item in request.Items)
        {
            var product = productMap[item.ProductId];
            try
            {
                product.ReserveStock(item.Quantity);
            }
            catch (DomainException exception)
            {
                return Result.Failure<Guid>(Error.Conflict(exception.Message));
            }

            lines.Add(new OrderLine(
                product.Id,
                Sku.Create(product.Sku.Value),
                product.Name,
                Money.Of(product.Price.Amount, product.Price.Currency),
                item.Quantity));
        }

        var address = Address.Create(
            request.ShippingAddress.Street,
            request.ShippingAddress.Number,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.ZipCode,
            request.ShippingAddress.Country);

        var order = Order.Place(
            _currentUser.UserId.Value,
            address,
            lines,
            DiscountStrategyFactory.Create(request.Coupon),
            _clock.UtcNow);

        await _orders.AddAsync(order, cancellationToken);
        return Result.Success(order.Id);
    }
}
