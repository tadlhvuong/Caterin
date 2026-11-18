namespace Website.Models.Shop
{
    public static class MockProductCatalog
    {
        private static readonly List<ShopProductViewModel> _products =
        [
            new()
        {
            Id = 1,
            Slug = "nuoc-rua-tay-huong-sen",
            Name = "Nước rửa tay hương sen",
            CategorySlug = "nuoc-rua-tay",
            CategoryName = "Nước rửa tay",
            ImageUrl = "~/images/Demos/product-two-img1.png",
            Price = 89000,
            OriginalPrice = 109000,
            Rating = 4.9,
            ReviewCount = 128,
            SoldCount = 850,
            StockQuantity = 100,
            Volume = "500ml",
            Badge = "Bán chạy",
            BadgeColor = "success",
            IsFeatured = true
        },
        new()
        {
            Id = 2,
            Slug = "nuoc-rua-tay-huong-gao",
            Name = "Nước rửa tay hương gạo",
            CategorySlug = "nuoc-rua-tay",
            CategoryName = "Nước rửa tay",
            ImageUrl = "~/images/Demos/product-two-img2.png",
            Price = 79000,
            Rating = 4.7,
            ReviewCount = 86,
            SoldCount = 520,
            StockQuantity = 80,
            Volume = "500ml"
        },
        new()
        {
            Id = 3,
            Slug = "nuoc-rua-tay-tao-kiwi",
            Name = "Nước rửa tay táo kiwi",
            CategorySlug = "nuoc-rua-tay",
            CategoryName = "Nước rửa tay",
            ImageUrl = "~/images/Demos/product-two-img3.png",
            Price = 85000,
            Rating = 4.8,
            ReviewCount = 64,
            SoldCount = 310,
            StockQuantity = 75,
            Volume = "500ml",
            IsNew = true
        },
        new()
        {
            Id = 4,
            Slug = "nuoc-rua-chen-cam-queb",
            Name = "Nước rửa chén cam quế",
            CategorySlug = "nuoc-rua-chen",
            CategoryName = "Nước rửa chén",
            ImageUrl = "~/images/Demos/product-two-img4.png",
            Price = 95000,
            OriginalPrice = 115000,
            Rating = 4.9,
            ReviewCount = 156,
            SoldCount = 920,
            StockQuantity = 120,
            Volume = "750ml",
            Badge = "Bán chạy",
            BadgeColor = "success",
            IsFeatured = true
        },
        new()
        {
            Id = 5,
            Slug = "nuoc-rua-chen-sa-chanh",
            Name = "Nước rửa chén sả chanh",
            CategorySlug = "nuoc-rua-chen",
            CategoryName = "Nước rửa chén",
            ImageUrl = "~/images/Demos/product-two-img5.png",
            Price = 89000,
            Rating = 4.6,
            ReviewCount = 72,
            SoldCount = 430,
            StockQuantity = 90,
            Volume = "750ml"
        },
        new()
        {
            Id = 6,
            Slug = "nuoc-rua-chen-huong-gao",
            Name = "Nước rửa chén hương gạo",
            CategorySlug = "nuoc-rua-chen",
            CategoryName = "Nước rửa chén",
            ImageUrl = "~/images/Demos/product-two-img6.png",
            Price = 99000,
            Rating = 4.8,
            ReviewCount = 91,
            SoldCount = 390,
            StockQuantity = 85,
            Volume = "750ml",
            IsNew = true
        },
        new()
        {
            Id = 7,
            Slug = "nuoc-lau-san-huong-lavender",
            Name = "Nước lau sàn hương Lavender",
            CategorySlug = "nuoc-lau-san",
            CategoryName = "Nước lau sàn",
            ImageUrl = "~/images/Demos/product-two-img7.png",
            Price = 105000,
            OriginalPrice = 125000,
            Rating = 4.9,
            ReviewCount = 113,
            SoldCount = 680,
            StockQuantity = 100,
            Volume = "1L",
            Badge = "Bán chạy",
            BadgeColor = "success",
            IsFeatured = true
        },
        new()
        {
            Id = 8,
            Slug = "nuoc-lau-san-huong-ylang",
            Name = "Nước lau sàn hương Ylang",
            CategorySlug = "nuoc-lau-san",
            CategoryName = "Nước lau sàn",
            ImageUrl = "~/images/Demos/product-two-img8.png",
            Price = 109000,
            Rating = 4.7,
            ReviewCount = 58,
            SoldCount = 270,
            StockQuantity = 65,
            Volume = "1L"
        },
        new()
        {
            Id = 9,
            Slug = "nuoc-lau-san-hoa-ha",
            Name = "Nước lau sàn Hoa Hạ",
            CategorySlug = "nuoc-lau-san",
            CategoryName = "Nước lau sàn",
            ImageUrl = "~/images/Demos/product-two-img9.png",
            Price = 99000,
            Rating = 4.6,
            ReviewCount = 43,
            SoldCount = 190,
            StockQuantity = 70,
            Volume = "1L",
            IsNew = true
        }
        ];

        public static IReadOnlyList<ShopProductViewModel> Products => _products;

        public static ShopProductViewModel? GetById(int id)
        {
            return _products.FirstOrDefault(x => x.Id == id);
        }

        public static List<ShopProductViewModel> GetByIds(IEnumerable<int> ids)
        {
            var idSet = ids.ToHashSet();

            return _products
                .Where(x => idSet.Contains(x.Id))
                .ToList();
        }
    }
}
