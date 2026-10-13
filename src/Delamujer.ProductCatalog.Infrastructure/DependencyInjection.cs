using Delamujer.ProductCatalog.Domain.Repositories;
using Delamujer.ProductCatalog.Infrastructure.Persistence;
using Delamujer.ProductCatalog.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delamujer.ProductCatalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "PostgreSQL";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ProductCatalogDbContext>(options =>
                options.UseSqlite(connectionString ?? "Data Source=productcatalog.db"));
        }
        else
        {
            services.AddDbContext<ProductCatalogDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProductCatalogDbContext>());

        return services;
    }
}
