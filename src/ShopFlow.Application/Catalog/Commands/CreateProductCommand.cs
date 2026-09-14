using FluentValidation;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.BuildingBlocks.Results;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Application.Catalog.Commands;

public sealed record CreateProductCommand(string Name, string Sku, decimal Price, string Currency, int Stock)
    : ICommand<Guid>, ITransactionalRequest;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Sku).NotEmpty().MinimumLength(3);
        RuleFor(command => command.Price).GreaterThan(0);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.Stock).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateProductCommandHandler : MediatR.IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _products;

    public CreateProductCommandHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var sku = Sku.Create(request.Sku);
        if (await _products.GetBySkuAsync(sku, cancellationToken) is not null)
        {
            return Result.Failure<Guid>(Error.Conflict($"SKU {sku.Value} já existe."));
        }

        var product = Product.Create(request.Name, sku, Money.Of(request.Price, request.Currency), request.Stock);
        await _products.AddAsync(product, cancellationToken);
        return Result.Success(product.Id);
    }
}
