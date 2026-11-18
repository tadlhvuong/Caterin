
namespace Website.Models.Shop
{
    public class ShopProductViewModel
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }

        public double Rating { get; set; }
        public int ReviewCount { get; set; }

        public int SoldCount { get; set; }
        public int StockQuantity { get; set; }

        public string Volume { get; set; } = string.Empty;

        public string? Badge { get; set; }
        public string? BadgeColor { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsNew { get; set; }

        public int DiscountPercent =>
            OriginalPrice.HasValue && OriginalPrice.Value > Price
                ? (int)Math.Round(
                    (OriginalPrice.Value - Price) / OriginalPrice.Value * 100)
                : 0;
    }
}
