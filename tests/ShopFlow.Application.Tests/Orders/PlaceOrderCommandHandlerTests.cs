using FluentAssertions;
using Moq;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Abstractions.Security;
using ShopFlow.Application.Orders.Commands;
using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Ordering;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Application.Tests.Orders;

public class PlaceOrderCommandHandlerTests
{
    [Fact]
    public async Task Should_Place_Order_And_Reserve_Stock()
    {
        var product = Product.Create("Capacete", Sku.Create("CAP-001"), Money.Of(100), 5);
        var products = new Mock<IProductRepository>();
        products.Setup(item => item.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });
        var orders = new Mock<IOrderRepository>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(Guid.NewGuid());
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(item => item.UtcNow).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var handler = new PlaceOrderCommandHandler(products.Object, orders.Object, currentUser.Object, clock.Object);
        var command = new PlaceOrderCommand(
            [new PlaceOrderItem(product.Id, 2)],
            new PlaceOrderAddress("Rua A", "10", "São Paulo", "SP", "01000-000", "BR"),
            "WELCOME10");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(3);
        orders.Verify(item => item.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_User_Is_Not_Authenticated()
    {
        var handler = new PlaceOrderCommandHandler(
            new Mock<IProductRepository>().Object,
            new Mock<IOrderRepository>().Object,
            new Mock<ICurrentUser>().Object,
            new Mock<IDateTimeProvider>().Object);

        var result = await handler.Handle(
            new PlaceOrderCommand(
                [new PlaceOrderItem(Guid.NewGuid(), 1)],
                new PlaceOrderAddress("Rua A", "10", "São Paulo", "SP", "01000-000", "BR"),
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Should_Fail_When_Stock_Is_Insufficient()
    {
        var product = Product.Create("Capacete", Sku.Create("CAP-001"), Money.Of(100), 1);
        var products = new Mock<IProductRepository>();
        products.Setup(item => item.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(Guid.NewGuid());

        var handler = new PlaceOrderCommandHandler(
            products.Object,
            new Mock<IOrderRepository>().Object,
            currentUser.Object,
            new Mock<IDateTimeProvider>().Object);

        var result = await handler.Handle(
            new PlaceOrderCommand(
                [new PlaceOrderItem(product.Id, 5)],
                new PlaceOrderAddress("Rua A", "10", "São Paulo", "SP", "01000-000", "BR"),
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict");
    }
}
