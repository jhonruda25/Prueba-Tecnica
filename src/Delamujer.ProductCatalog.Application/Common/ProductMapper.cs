using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.Domain.Entities;

namespace Delamujer.ProductCatalog.Application.Common;

public static class ProductMapper
{
    public static ProductResponseDto ToDto(this Product product)
    {
        return new ProductResponseDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.IsActive,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
    }
}
