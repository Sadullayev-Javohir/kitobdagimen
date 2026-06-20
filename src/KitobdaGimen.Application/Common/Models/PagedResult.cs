namespace KitobdaGimen.Application.Common.Models;

/// <summary>A page of results together with pagination metadata.</summary>
public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
}
