
namespace Website.Models.Shop
{
    public class ShopIndexViewModel
    {
        public List<ShopProductViewModel> Products { get; set; } = [];

        public List<ShopCategoryFilterViewModel> Categories { get; set; } = [];
        public List<ShopVolumeFilterViewModel> Volumes { get; set; } = [];

        // Search and filters
        public string? Keyword { get; set; }
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Rating { get; set; }
        public string? Volume { get; set; }
        public string SortBy { get; set; } = "featured";

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalProducts { get; set; }
        public int TotalPages =>
            (int)Math.Ceiling(TotalProducts / (double)PageSize);

        public int StartItem =>
            TotalProducts == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;

        public int EndItem =>
            Math.Min(CurrentPage * PageSize, TotalProducts);

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Price range for the filter UI
        public decimal AvailableMinPrice { get; set; }
        public decimal AvailableMaxPrice { get; set; }
    }
}
