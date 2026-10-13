using Delamujer.ProductCatalog.Domain.Entities;
using Delamujer.ProductCatalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Delamujer.ProductCatalog.Infrastructure.Persistence;

public class ProductCatalogDbContext : DbContext, IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();

    public ProductCatalogDbContext(DbContextOptions<ProductCatalogDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductCatalogDbContext).Assembly);
    }
}
