namespace Website.Areas.Admin.Models.Product
{
    public class ProductListViewModel
    {
        public int Id { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Slug { get; set; }

        public string? PrimaryImageUrl { get; set; }

        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public decimal Price { get; set; }

        public decimal? CompareAtPrice { get; set; }

        public int Stock { get; set; }

        public bool IsActive { get; set; }

        public bool IsFeatured { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
