using FluentAssertions;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Catalog.Events;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Tests.Catalog;

public class ProductTests
{
    [Fact]
    public void Create_Should_Raise_ProductCreated_Event()
    {
        var product = Product.Create("Capacete", Sku.Create("CAP-001"), Money.Of(199), 10);

        product.IsActive.Should().BeTrue();
        product.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ProductCreatedDomainEvent);
    }

    [Fact]
    public void ReserveStock_Should_Decrease_Quantity()
    {
        var product = Product.Create("Capacete", Sku.Create("CAP-001"), Money.Of(199), 10);

        product.ReserveStock(3);

        product.Stock.Should().Be(7);
    }

    [Fact]
    public void ReserveStock_Should_Fail_When_Insufficient()
    {
        var product = Product.Create("Capacete", Sku.Create("CAP-001"), Money.Of(199), 2);

        var act = () => product.ReserveStock(3);

        act.Should().Throw<DomainException>().Where(error => error.Code == "Product.InsufficientStock");
    }

    [Fact]
    public void Inactive_Product_Cannot_Be_Sold()
    {
        var product = Product.Create("Capacete", Sku.Create("CAP-001"), Money.Of(199), 10);
        product.Deactivate();

        var act = () => product.ReserveStock(1);

        act.Should().Throw<DomainException>().Where(error => error.Code == "Product.Inactive");
    }
}
