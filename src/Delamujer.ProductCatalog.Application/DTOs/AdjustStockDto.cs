namespace Delamujer.ProductCatalog.Application.DTOs;

/// <summary>
/// Payload to adjust product stock. Supports positive (increment) or negative (decrement) values.
/// </summary>
public record AdjustStockDto(int QuantityDelta);
