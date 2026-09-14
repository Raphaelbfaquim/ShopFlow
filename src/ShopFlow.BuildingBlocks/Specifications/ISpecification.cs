using System.Linq.Expressions;

namespace ShopFlow.BuildingBlocks.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();

    bool IsSatisfiedBy(T entity);
}
