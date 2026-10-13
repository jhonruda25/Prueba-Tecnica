namespace Delamujer.ProductCatalog.Application.DTOs;

public record ProductQueryParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    private int _pageNumber = 1;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 10 : (value > MaxPageSize ? MaxPageSize : value);
    }

    public string? Search { get; init; }
    public bool? IsActive { get; init; } = true;
}
