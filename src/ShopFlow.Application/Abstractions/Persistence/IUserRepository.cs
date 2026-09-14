using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.Domain.Identity;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Application.Abstractions.Persistence;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default);
}
