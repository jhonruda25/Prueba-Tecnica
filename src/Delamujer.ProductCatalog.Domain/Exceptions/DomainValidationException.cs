namespace Delamujer.ProductCatalog.Domain.Exceptions;

/// <summary>
/// Thrown when entity invariants or domain validation rules are violated.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
