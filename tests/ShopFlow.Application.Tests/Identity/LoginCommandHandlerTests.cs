using FluentAssertions;
using Moq;
using ShopFlow.Application.Abstractions.Persistence;
using ShopFlow.Application.Abstractions.Security;
using ShopFlow.Application.Identity.Commands;
using ShopFlow.Domain.Identity;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Application.Tests.Identity;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Should_Return_Token_When_Credentials_Are_Valid()
    {
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(item => item.Verify("Senha@123", "hash")).Returns(true);
        var user = User.Register("Ana", Email.Create("ana@shopflow.dev"), "hash");
        var users = new Mock<IUserRepository>();
        users.Setup(item => item.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(item => item.CreateToken(user.Id, user.Email.Value, user.Role)).Returns("token");

        var handler = new LoginCommandHandler(users.Object, hasher.Object, jwt.Object);
        var result = await handler.Handle(new LoginCommand("ana@shopflow.dev", "Senha@123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("token");
    }

    [Fact]
    public async Task Should_Fail_When_Password_Is_Wrong()
    {
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(item => item.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var user = User.Register("Ana", Email.Create("ana@shopflow.dev"), "hash");
        var users = new Mock<IUserRepository>();
        users.Setup(item => item.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = new LoginCommandHandler(users.Object, hasher.Object, new Mock<IJwtTokenService>().Object);
        var result = await handler.Handle(new LoginCommand("ana@shopflow.dev", "errada"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized");
    }
}
