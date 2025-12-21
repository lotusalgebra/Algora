namespace Algora.Application.DTOs.Communication;

public record PaginatedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PaginatedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
        => new() { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
}