namespace Delamujer.ProductCatalog.Domain.Exceptions;

/// <summary>
/// Base exception for all business domain errors.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
