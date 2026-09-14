using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ShopFlow.Infrastructure.Persistence;

namespace ShopFlow.Infrastructure.Persistence;

public sealed class ShopFlowDbContextFactory : IDesignTimeDbContextFactory<ShopFlowDbContext>
{
    public ShopFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShopFlowDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=ShopFlow;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False;")
            .Options;

        return new ShopFlowDbContext(options);
    }
}
