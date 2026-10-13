using Delamujer.ProductCatalog.Domain.Entities;
using Delamujer.ProductCatalog.Domain.Exceptions;
using Delamujer.ProductCatalog.Domain.Repositories;
using Delamujer.ProductCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Delamujer.ProductCatalog.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductCatalogDbContext _context;

    public ProductRepository(ProductCatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public void Delete(Product product)
    {
        _context.Products.Remove(product);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        bool? onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (onlyActive.HasValue)
        {
            query = query.Where(p => p.IsActive == onlyActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                     p.Description.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Product?> AdjustStockAtomicAsync(Guid id, int delta, CancellationToken cancellationToken = default)
    {
        if (delta == 0)
        {
            throw new DomainValidationException("Stock adjustment delta cannot be zero.");
        }

        var now = DateTimeOffset.UtcNow;

        // Atomic database-level update: updates only if resulting stock >= 0
        var rowsAffected = await _context.Products
            .Where(p => p.Id == id && (p.Stock + delta) >= 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Stock, p => p.Stock + delta)
                .SetProperty(p => p.UpdatedAtUtc, now),
                cancellationToken);

        if (rowsAffected == 1)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        // Differentiate between non-existent product vs insufficient stock
        var existing = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        throw new InsufficientStockException(existing.Id, existing.Stock, delta);
    }
}
