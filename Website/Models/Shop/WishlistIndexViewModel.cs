
namespace Website.Models.Wishlist
{
    public class WishlistIndexViewModel
    {
        public List<WishlistItemViewModel> Items { get; set; } = [];

        public int TotalItems => Items.Count;
    }

    public class WishlistItemViewModel
    {
        public int Id { get; set; }

        public string Slug { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string Volume { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }

        public double Rating { get; set; }

        public int ReviewCount { get; set; }

        public int StockQuantity { get; set; }

        public bool IsAvailable => StockQuantity > 0;
    }
}
