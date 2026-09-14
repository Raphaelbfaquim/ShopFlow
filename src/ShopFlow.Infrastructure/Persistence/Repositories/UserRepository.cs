using Microsoft.EntityFrameworkCore;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Domain.Identity;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ShopFlowDbContext _dbContext;

    public UserRepository(ShopFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task AddAsync(User aggregate, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(aggregate, cancellationToken);
    }
}
