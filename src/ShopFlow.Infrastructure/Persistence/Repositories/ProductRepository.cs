using Microsoft.EntityFrameworkCore;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ShopFlowDbContext _dbContext;

    public ProductRepository(ShopFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.FirstOrDefaultAsync(product => product.Sku == sku, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _dbContext.Products.Where(product => idList.Contains(product.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products.Where(product => product.IsActive).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product aggregate, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(aggregate, cancellationToken);
    }
}
