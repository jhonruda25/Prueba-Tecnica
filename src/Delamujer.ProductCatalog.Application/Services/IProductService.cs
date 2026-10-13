using Delamujer.ProductCatalog.Application.DTOs;

namespace Delamujer.ProductCatalog.Application.Services;

public interface IProductService
{
    Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProductResponseDto>> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> AdjustStockAsync(Guid id, AdjustStockDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
