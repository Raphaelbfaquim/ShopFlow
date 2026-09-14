using FluentAssertions;
using NetArchTest.Rules;
using ShopFlow.Domain.Ordering;

namespace ShopFlow.Architecture.Tests;

public class LayeringTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Or_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Order).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("ShopFlow.Application", "ShopFlow.Infrastructure", "ShopFlow.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(ShopFlow.Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("ShopFlow.Infrastructure", "ShopFlow.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Frameworks()
    {
        var result = Types.InAssembly(typeof(Order).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "MediatR",
                "Dapper",
                "RabbitMQ.Client",
                "Azure.Storage.Blobs")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_Persistence()
    {
        var result = Types.InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace("ShopFlow.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("ShopFlow.Infrastructure.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
