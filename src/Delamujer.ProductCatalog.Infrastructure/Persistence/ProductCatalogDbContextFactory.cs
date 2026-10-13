using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Delamujer.ProductCatalog.Infrastructure.Persistence;

public class ProductCatalogDbContextFactory : IDesignTimeDbContextFactory<ProductCatalogDbContext>
{
    public ProductCatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductCatalogDbContext>();

        // Default design-time provider for generating migrations
        optionsBuilder.UseNpgsql("Host=localhost;Database=delamujer_catalog;Username=postgres;Password=postgres");

        return new ProductCatalogDbContext(optionsBuilder.Options);
    }
}
