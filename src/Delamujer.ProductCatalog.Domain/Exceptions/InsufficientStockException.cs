namespace Delamujer.ProductCatalog.Domain.Exceptions;

/// <summary>
/// Thrown when an inventory deduction exceeds current available stock.
/// </summary>
public sealed class InsufficientStockException : DomainException
{
    public Guid ProductId { get; }
    public int CurrentStock { get; }
    public int RequestedDelta { get; }

    public InsufficientStockException(Guid productId, int currentStock, int requestedDelta)
        : base($"Cannot adjust stock for product '{productId}'. Current stock is {currentStock}, requested change is {requestedDelta}. Resulting stock cannot be negative.")
    {
        ProductId = productId;
        CurrentStock = currentStock;
        RequestedDelta = requestedDelta;
    }
}
