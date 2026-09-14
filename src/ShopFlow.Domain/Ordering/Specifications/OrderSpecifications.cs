using System.Linq.Expressions;
using ShopFlow.BuildingBlocks.Specifications;

namespace ShopFlow.Domain.Ordering.Specifications;

public sealed class PendingPaymentOrdersSpecification : Specification<Order>
{
    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Status == OrderStatus.Placed;
    }
}

public sealed class CustomerOrdersSpecification : Specification<Order>
{
    private readonly Guid _customerId;

    public CustomerOrdersSpecification(Guid customerId)
    {
        _customerId = customerId;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.CustomerId == _customerId;
    }
}
