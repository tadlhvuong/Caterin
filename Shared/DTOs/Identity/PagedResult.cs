namespace Shared.DTOs.Identity
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; init; }

        public int FilteredCount { get; init; }

        public int TotalPages =>
            PageSize > 0
                ? (int)Math.Ceiling((double)FilteredCount / PageSize)
                : 0;

        public bool HasPreviousPage => Page > 1;

        public bool HasNextPage => Page < TotalPages;
    }
}
