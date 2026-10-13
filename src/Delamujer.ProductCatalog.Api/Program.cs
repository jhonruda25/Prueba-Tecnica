using System.Reflection;
using Delamujer.ProductCatalog.Api.Middleware;
using Delamujer.ProductCatalog.Application;
using Delamujer.ProductCatalog.Infrastructure;
using Delamujer.ProductCatalog.Infrastructure.Persistence;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fundación delamujer - API de Catálogo de Productos y Stock",
        Version = "v1",
        Description = "API REST backend lista para producción que gestiona el catálogo de productos e inventario concurrente con Clean Architecture en .NET 10.",
        Contact = new OpenApiContact
        {
            Name = "Analista Desarrollador de Aplicaciones",
            Email = "desarrollo@fundaciondelamujer.com",
            Url = new Uri("https://www.fundaciondelamujer.com")
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Global Exception Handler
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Always enable Swagger UI so public deployments provide interactive documentation out-of-the-box
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fundación delamujer API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at root URL
    c.DocumentTitle = "Fundación delamujer - Catálogo y Stock API";
});

app.UseCors("AllowAll");

app.MapHealthChecks("/health");
app.MapControllers();

// Execute automatic migrations and data seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ProductCatalogDbContext>();
        await DatabaseSeeder.SeedAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations or seed initial data.");
    }
}

app.Run();

public partial class Program { }
