using Delamujer.ProductCatalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Delamujer.ProductCatalog.IntegrationTests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection($"Data Source=InMemoryDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared;");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout = 10000; PRAGMA journal_mode = WAL;";
        cmd.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("DatabaseProvider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connection.ConnectionString);

        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ProductCatalogDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ProductCatalogDbContext)
            ).ToList();

            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            services.AddDbContext<ProductCatalogDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

            db.Database.EnsureCreated();
            DatabaseSeeder.SeedAsync(db, logger).GetAwaiter().GetResult();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Close();
        _connection.Dispose();
    }
}
