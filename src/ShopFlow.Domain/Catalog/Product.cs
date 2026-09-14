using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.BuildingBlocks.Guards;
using ShopFlow.Domain.Catalog.Events;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Catalog;

public sealed class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;

    public Sku Sku { get; private set; } = null!;

    public Money Price { get; private set; } = null!;

    public int Stock { get; private set; }

    public bool IsActive { get; private set; }

    private Product()
    {
    }

    private Product(Guid id, string name, Sku sku, Money price, int stock)
        : base(id)
    {
        Name = name;
        Sku = sku;
        Price = price;
        Stock = stock;
        IsActive = true;
    }

    public static Product Create(string name, Sku sku, Money price, int stock)
    {
        var product = new Product(
            Guid.NewGuid(),
            Guard.AgainstNullOrWhiteSpace(name, nameof(name)),
            Guard.AgainstNull(sku, nameof(sku)),
            Guard.AgainstNull(price, nameof(price)),
            Guard.AgainstNegative(stock, nameof(stock)));

        product.Raise(new ProductCreatedDomainEvent(product.Id, product.Sku.Value, product.Name));
        return product;
    }

    public void ChangePrice(Money price)
    {
        if (!IsActive)
        {
            throw new DomainException("Product.Inactive", "Produto inativo não pode ter preço alterado.");
        }

        Price = Guard.AgainstNull(price, nameof(price));
    }

    public void ReserveStock(int quantity)
    {
        EnsureActive();
        Guard.AgainstZeroOrNegative(quantity, nameof(quantity));

        if (Stock < quantity)
        {
            throw new DomainException("Product.InsufficientStock", $"Estoque insuficiente para o SKU {Sku.Value}.");
        }

        Stock -= quantity;
        Raise(new StockAdjustedDomainEvent(Id, -quantity, Stock));
    }

    public void ReleaseStock(int quantity)
    {
        Guard.AgainstZeroOrNegative(quantity, nameof(quantity));
        Stock += quantity;
        Raise(new StockAdjustedDomainEvent(Id, quantity, Stock));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException("Product.Inactive", "Produto inativo não pode ser vendido.");
        }
    }
}
