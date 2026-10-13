using Delamujer.ProductCatalog.Application.Common;
using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.Domain.Entities;
using Delamujer.ProductCatalog.Domain.Exceptions;
using Delamujer.ProductCatalog.Domain.Repositories;
using FluentValidation;

namespace Delamujer.ProductCatalog.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;
    private readonly IValidator<AdjustStockDto> _adjustStockValidator;

    public ProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator,
        IValidator<AdjustStockDto> adjustStockValidator)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _adjustStockValidator = adjustStockValidator;
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var product = Product.Create(dto.Name, dto.Description, dto.Price, dto.InitialStock);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task<ProductResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            throw new ProductNotFoundException(id);
        }

        return product.ToDto();
    }

    public async Task<PagedResultDto<ProductResponseDto>> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(
            query.PageNumber,
            query.PageSize,
            query.Search,
            query.IsActive,
            cancellationToken);

        var dtos = items.Select(p => p.ToDto()).ToList();
        return PagedResultDto<ProductResponseDto>.Create(dtos, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<ProductResponseDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            throw new ProductNotFoundException(id);
        }

        product.UpdateDetails(dto.Name, dto.Description, dto.Price);
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task<ProductResponseDto> AdjustStockAsync(Guid id, AdjustStockDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _adjustStockValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Delegate to repository atomic adjustment for race condition prevention
        var updatedProduct = await _productRepository.AdjustStockAtomicAsync(id, dto.QuantityDelta, cancellationToken);
        if (updatedProduct is null)
        {
            throw new ProductNotFoundException(id);
        }

        return updatedProduct.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            throw new ProductNotFoundException(id);
        }

        product.Deactivate();
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
