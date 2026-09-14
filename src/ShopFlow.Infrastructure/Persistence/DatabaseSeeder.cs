using Microsoft.EntityFrameworkCore;
using ShopFlow.Application.Abstractions.Security;
using ShopFlow.Domain.Catalog;
using ShopFlow.Domain.Identity;
using ShopFlow.Domain.Shared;
using ShopFlow.Infrastructure.Persistence;

namespace ShopFlow.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ShopFlowDbContext dbContext, IPasswordHasher passwordHasher)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var admin = User.Register("Admin ShopFlow", Email.Create("admin@shopflow.dev"), passwordHasher.Hash("Admin@123"), UserRoles.Admin);
        var customer = User.Register("Cliente Demo", Email.Create("cliente@shopflow.dev"), passwordHasher.Hash("Cliente@123"), UserRoles.Customer);

        var helmet = Product.Create("Capacete Fechado", Sku.Create("CAP-001"), Money.Of(249.90m), 25);
        var jacket = Product.Create("Jaqueta Motociclista", Sku.Create("JAQ-002"), Money.Of(499.00m), 12);
        var gloves = Product.Create("Luva Couro", Sku.Create("LUV-003"), Money.Of(89.90m), 40);

        await dbContext.Users.AddRangeAsync(admin, customer);
        await dbContext.Products.AddRangeAsync(helmet, jacket, gloves);
        await dbContext.SaveChangesAsync();
    }
}
