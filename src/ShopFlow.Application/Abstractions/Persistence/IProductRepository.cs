using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Application.Abstractions.Persistence;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken cancellationToken = default);
}
