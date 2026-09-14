using FluentAssertions;
using ShopFlow.Domain.Identity;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Tests.Identity;

public class UserTests
{
    [Fact]
    public void Register_Should_Create_Active_Customer()
    {
        var user = User.Register("Ana", Email.Create("ana@shopflow.dev"), "hash");

        user.IsActive.Should().BeTrue();
        user.Role.Should().Be(UserRoles.Customer);
        user.DomainEvents.Should().HaveCount(1);
    }
}
