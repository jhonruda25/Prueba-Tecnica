namespace Delamujer.ProductCatalog.Domain.Exceptions;

/// <summary>
/// Thrown when a product query or operation targets a non-existent entity.
/// </summary>
public sealed class ProductNotFoundException : DomainException
{
    public Guid ProductId { get; }

    public ProductNotFoundException(Guid productId)
        : base($"Product with ID '{productId}' was not found.")
    {
        ProductId = productId;
    }
}
