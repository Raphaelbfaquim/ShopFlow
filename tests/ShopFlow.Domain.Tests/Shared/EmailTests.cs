using FluentAssertions;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Tests.Shared;

public class EmailTests
{
    [Fact]
    public void Create_Should_Normalize_Email()
    {
        var email = Email.Create("  User@ShopFlow.DEV ");

        email.Value.Should().Be("user@shopflow.dev");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalido")]
    [InlineData("a@b")]
    public void Create_Should_Reject_Invalid_Email(string value)
    {
        var act = () => Email.Create(value);

        act.Should().Throw<DomainException>();
    }
}
