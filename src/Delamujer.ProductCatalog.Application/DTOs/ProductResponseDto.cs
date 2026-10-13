namespace Delamujer.ProductCatalog.Application.DTOs;

public record ProductResponseDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
