using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.RateLimiting;
using Delamujer.ProductCatalog.Api.Middleware;
using Delamujer.ProductCatalog.Application;
using Delamujer.ProductCatalog.Infrastructure;
using Delamujer.ProductCatalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add architectural layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Database-backed Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductCatalogDbContext>(
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["db", "ready"]);

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Native Rate Limiting (Defense-in-depth against DoS and transactional flooding)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120, // 120 requests per minute per client
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Type = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
            Detail = "Rate limit threshold exceeded. Please retry after a short period.",
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.HttpContext.Response.WriteAsync(json, cancellationToken);
    };
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

// Pipeline Middlewares
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseRateLimiter();

// Always serve interactive documentation on root path
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fundación delamujer API v1");
    c.RoutePrefix = string.Empty;
    c.DocumentTitle = "Fundación delamujer - Catálogo y Stock API";
});

app.UseCors("AllowAll");

app.MapHealthChecks("/health");
app.MapGet("/swagger", () => Results.Redirect("/"));
app.MapControllers();

// Execute automated migrations and seeding
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
