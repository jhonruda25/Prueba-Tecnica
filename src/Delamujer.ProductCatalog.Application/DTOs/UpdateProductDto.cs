namespace Delamujer.ProductCatalog.Application.DTOs;

public record UpdateProductDto(
    string Name,
    string? Description,
    decimal Price);
