using ShopFlow.Application.Abstractions.Caching;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.BuildingBlocks.Results;
using ShopFlow.Domain.Catalog;

namespace ShopFlow.Application.Catalog.Queries;

public sealed record ProductDto(Guid Id, string Name, string Sku, decimal Price, string Currency, int Stock, bool IsActive);

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;

public sealed class GetProductByIdQueryHandler : MediatR.IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IProductRepository _products;
    private readonly ICacheService _cache;

    public GetProductByIdQueryHandler(IProductRepository products, ICacheService cache)
    {
        _products = products;
        _cache = cache;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"product:{request.ProductId}";
        var cached = await _cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound(nameof(Product), request.ProductId));
        }

        var dto = Map(product);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), cancellationToken);
        return Result.Success(dto);
    }

    public static ProductDto Map(Product product) =>
        new(product.Id, product.Name, product.Sku.Value, product.Price.Amount, product.Price.Currency, product.Stock, product.IsActive);
}

public sealed record ListProductsQuery : IQuery<IReadOnlyList<ProductDto>>;

public sealed class ListProductsQueryHandler : MediatR.IRequestHandler<ListProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IProductRepository _products;

    public ListProductsQueryHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _products.ListActiveAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ProductDto>>(products.Select(GetProductByIdQueryHandler.Map).ToList());
    }
}
