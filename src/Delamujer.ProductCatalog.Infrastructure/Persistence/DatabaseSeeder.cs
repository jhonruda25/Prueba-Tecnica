using Delamujer.ProductCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Delamujer.ProductCatalog.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ProductCatalogDbContext context, ILogger logger)
    {
        try
        {
            var isPostgreSql = context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

            if (isPostgreSql)
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            if (!await context.Products.AnyAsync())
            {
                logger.LogInformation("Database is empty. Seeding initial catalog products for Fundación delamujer...");

                var seedProducts = new[]
                {
                    Product.Create(
                        "Microcrédito AgroMujer",
                        "Financiamiento productivo para mujeres rurales dedicadas a la producción agropecuaria.",
                        2500000m,
                        50),
                    Product.Create(
                        "Crédito Emprende Mujer",
                        "Línea de microfinanzas para fortalecimiento y capital de trabajo en microempresas urbanas.",
                        1500000m,
                        80),
                    Product.Create(
                        "Seguro Integral Familia Protegida",
                        "Póliza exequial y de salud con cobertura familiar y asistencia médica 24/7.",
                        45000m,
                        200),
                    Product.Create(
                        "Línea Ahorro Semilla",
                        "Producto financiero programado para fomento del ahorro e inversión comunitaria.",
                        100000m,
                        150),
                    Product.Create(
                        "Financiamiento Mejoramiento Vivienda",
                        "Crédito con tasas preferenciales para adecuación y reforma de vivienda productiva.",
                        3200000m,
                        30)
                };

                await context.Products.AddRangeAsync(seedProducts);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} products successfully.", seedProducts.Length);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding or migrating the database.");
            throw;
        }
    }
}
