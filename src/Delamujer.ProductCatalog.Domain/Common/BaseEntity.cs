namespace Delamujer.ProductCatalog.Domain.Common;

/// <summary>
/// Base class for domain entities with audit and identifier attributes.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }
}
