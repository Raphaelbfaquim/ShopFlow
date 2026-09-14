using FluentValidation;
using ShopFlow.Application.Abstractions.Messaging;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Abstractions.Security;
using ShopFlow.BuildingBlocks.Abstractions;
using ShopFlow.BuildingBlocks.Results;
using ShopFlow.Domain.Identity;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Application.Identity.Commands;

public sealed record RegisterUserCommand(string Name, string Email, string Password, string? Role)
    : ICommand<Guid>, ITransactionalRequest;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class RegisterUserCommandHandler : MediatR.IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        if (await _users.ExistsAsync(email, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("E-mail já cadastrado."));
        }

        var role = string.IsNullOrWhiteSpace(request.Role) ? UserRoles.Customer : request.Role;
        var user = User.Register(request.Name, email, _passwordHasher.Hash(request.Password), role);
        await _users.AddAsync(user, cancellationToken);
        return Result.Success(user.Id);
    }
}
