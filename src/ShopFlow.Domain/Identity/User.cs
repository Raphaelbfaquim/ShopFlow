using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.BuildingBlocks.Guards;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Identity.Events;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Identity;

public sealed class User : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;

    public Email Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = string.Empty;

    public string Role { get; private set; } = UserRoles.Customer;

    public bool IsActive { get; private set; }

    private User()
    {
    }

    private User(Guid id, string name, Email email, string passwordHash, string role)
        : base(id)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    public static User Register(string name, Email email, string passwordHash, string role = UserRoles.Customer)
    {
        EnsureValidRole(role);

        var user = new User(
            Guid.NewGuid(),
            Guard.AgainstNullOrWhiteSpace(name, nameof(name)),
            Guard.AgainstNull(email, nameof(email)),
            Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash)),
            role);

        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email.Value));
        return user;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void EnsureValidRole(string role)
    {
        if (role is not (UserRoles.Admin or UserRoles.Customer))
        {
            throw new DomainException("User.Role", "Perfil de usuário inválido.");
        }
    }
}
