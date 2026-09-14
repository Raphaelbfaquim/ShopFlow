using FluentAssertions;
using Moq;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Catalog.Commands;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Application.Tests.Catalog;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Product_When_Sku_Is_Unique()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(item => item.GetBySkuAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        var handler = new CreateProductCommandHandler(repository.Object);

        var result = await handler.Handle(
            new CreateProductCommand("Capacete", "CAP-100", 199.9m, "BRL", 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Verify(item => item.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Sku_Already_Exists()
    {
        var existing = Product.Create("Outro", Sku.Create("CAP-100"), Money.Of(10), 1);
        var repository = new Mock<IProductRepository>();
        repository.Setup(item => item.GetBySkuAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var handler = new CreateProductCommandHandler(repository.Object);

        var result = await handler.Handle(
            new CreateProductCommand("Capacete", "CAP-100", 199.9m, "BRL", 5),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict");
    }
}
