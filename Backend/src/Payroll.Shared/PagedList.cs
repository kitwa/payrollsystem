namespace Payroll.Shared;

/// <summary>Generic paginated list for use across all query handlers.</summary>
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    private PagedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static PagedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var items = source.ToList();
        var totalCount = items.Count;
        var paged = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<T>(paged, totalCount, pageNumber, pageSize);
    }
}
