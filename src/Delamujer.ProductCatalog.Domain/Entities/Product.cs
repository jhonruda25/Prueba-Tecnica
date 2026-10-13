using Delamujer.ProductCatalog.Domain.Common;
using Delamujer.ProductCatalog.Domain.Exceptions;

namespace Delamujer.ProductCatalog.Domain.Entities;

/// <summary>
/// Core aggregate entity representing a product with inventory tracking.
/// </summary>
public class Product : BaseEntity
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 1000;

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Concurrency token for optimistic locking in high-throughput concurrent environments.
    /// </summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Parameterless constructor for ORM materialization
    protected Product() { }

    private Product(Guid id, string name, string? description, decimal price, int initialStock)
    {
        Id = id;
        ValidateName(name);
        ValidatePrice(price);
        ValidateInitialStock(initialStock);

        Name = name.Trim();
        Description = (description ?? string.Empty).Trim();
        Price = price;
        Stock = initialStock;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Factory method to instantiate a validated Product aggregate.
    /// </summary>
    public static Product Create(string name, string? description, decimal price, int initialStock)
    {
        return new Product(Guid.NewGuid(), name, description, price, initialStock);
    }

    /// <summary>
    /// Updates product general information while enforcing business constraints.
    /// </summary>
    public void UpdateDetails(string name, string? description, decimal price)
    {
        ValidateName(name);
        ValidatePrice(price);

        Name = name.Trim();
        Description = (description ?? string.Empty).Trim();
        Price = price;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds or subtracts units from current stock.
    /// Rejects operations that would drive the balance negative.
    /// </summary>
    /// <param name="delta">Units to add (positive) or deduct (negative).</param>
    public void AdjustStock(int delta)
    {
        if (delta == 0)
        {
            throw new DomainValidationException("Stock adjustment delta cannot be zero.");
        }

        int newStock = Stock + delta;
        if (newStock < 0)
        {
            throw new InsufficientStockException(Id, Stock, delta);
        }

        Stock = newStock;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Soft deletes the product, disabling it from active catalogs.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Restores a previously deactivated product.
    /// </summary>
    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Product name is required and cannot be empty.");
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new DomainValidationException($"Product name cannot exceed {MaxNameLength} characters.");
        }
    }

    private static void ValidatePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new DomainValidationException("Product price must be strictly greater than zero.");
        }
    }

    private static void ValidateInitialStock(int stock)
    {
        if (stock < 0)
        {
            throw new DomainValidationException("Initial stock cannot be negative.");
        }
    }
}
