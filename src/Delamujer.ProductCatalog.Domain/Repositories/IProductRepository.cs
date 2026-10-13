using Delamujer.ProductCatalog.Domain.Entities;

namespace Delamujer.ProductCatalog.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    void Delete(Product product);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        bool? onlyActive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an atomic, concurrency-safe database-level stock update.
    /// Returns the updated stock level if successful, or null if product does not exist.
    /// Throws InsufficientStockException if resulting balance would be negative.
    /// </summary>
    Task<Product?> AdjustStockAtomicAsync(Guid id, int delta, CancellationToken cancellationToken = default);
}
