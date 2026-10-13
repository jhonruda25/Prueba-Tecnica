using Delamujer.ProductCatalog.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Delamujer.ProductCatalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
