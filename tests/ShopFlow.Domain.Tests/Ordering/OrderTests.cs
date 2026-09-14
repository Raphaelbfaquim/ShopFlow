using FluentAssertions;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Ordering;
using ShopFlow.Domain.Ordering.Events;
using ShopFlow.Domain.Ordering.Policies;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Tests.Ordering;

public class OrderTests
{
    private static Order PlaceSample(string? coupon = null, int quantity = 1)
    {
        var lines = new[]
        {
            new OrderLine(Guid.NewGuid(), Sku.Create("CAP-001"), "Capacete", Money.Of(100), quantity)
        };

        return Order.Place(
            Guid.NewGuid(),
            Address.Create("Rua A", "10", "São Paulo", "SP", "01000-000"),
            lines,
            DiscountStrategyFactory.Create(coupon),
            new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Place_Should_Calculate_Total_And_Raise_Event()
    {
        var order = PlaceSample("WELCOME10");

        order.Subtotal.Amount.Should().Be(100);
        order.Discount.Amount.Should().Be(10);
        order.Total.Amount.Should().Be(90);
        order.Status.Should().Be(OrderStatus.Placed);
        order.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is OrderPlacedDomainEvent);
    }

    [Fact]
    public void Place_Should_Reject_Empty_Basket()
    {
        var act = () => Order.Place(
            Guid.NewGuid(),
            Address.Create("Rua A", "10", "São Paulo", "SP", "01000-000"),
            Array.Empty<OrderLine>(),
            new NoDiscountStrategy(),
            DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(error => error.Code == "Order.Empty");
    }

    [Fact]
    public void MarkAsPaid_Should_Transition_From_Placed()
    {
        var order = PlaceSample();
        order.ClearDomainEvents();

        order.MarkAsPaid(DateTime.UtcNow);

        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAtUtc.Should().NotBeNull();
        order.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is OrderPaidDomainEvent);
    }

    [Fact]
    public void Cancel_Should_Fail_After_Shipping()
    {
        var order = PlaceSample();
        order.MarkAsPaid(DateTime.UtcNow);
        order.MarkAsShipped();

        var act = () => order.Cancel(DateTime.UtcNow);

        act.Should().Throw<DomainException>().Where(error => error.Code == "Order.Cancel");
    }

    [Fact]
    public void Specification_Should_Match_Pending_Orders()
    {
        var pending = PlaceSample();
        var paid = PlaceSample();
        paid.MarkAsPaid(DateTime.UtcNow);

        var spec = new ShopFlow.Domain.Ordering.Specifications.PendingPaymentOrdersSpecification();

        spec.IsSatisfiedBy(pending).Should().BeTrue();
        spec.IsSatisfiedBy(paid).Should().BeFalse();
    }
}
