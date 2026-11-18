
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website.Models.Shop;

namespace Website.Controllers
{
    [AllowAnonymous]
    public class ShopController : Controller
    {
        private const int PageSize = 12;

        // Mock data: thay bằng service/database sau.
        
        [HttpGet("shop")]
        public IActionResult Index(
            string? keyword,
            string? category,
            decimal? minPrice,
            decimal? maxPrice,
            int? rating,
            string? volume,
            string sortBy = "featured",
            int page = 1)
        {
            // Giữ nguyên toàn bộ logic lọc, sắp xếp,
            // phân trang và tạo ShopIndexViewModel hiện tại.

            var model = BuildShopIndexViewModel(
                keyword,
                category,
                minPrice,
                maxPrice,
                rating,
                volume,
                sortBy,
                page);

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("_ShopProductResults", model);
            }

            return View(model);
        }

        [HttpGet("product/{slug}")]
        public IActionResult Details(string slug)
        {
            var product = MockProductCatalog.Products.FirstOrDefault(x =>
                x.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

            if (product == null)
                return NotFound();

            return View("~/Views/Shop/Details.cshtml", product);
        }

        private ShopIndexViewModel BuildShopIndexViewModel(
    string? keyword,
    string? category,
    decimal? minPrice,
    decimal? maxPrice,
    int? rating,
    string? volume,
    string? sortBy,
    int page)
        {
            const int pageSize = 12;

            var products = MockProductCatalog.Products.AsEnumerable();

            // Chuẩn hóa tham số.
            keyword = keyword?.Trim();
            category = category?.Trim();
            volume = volume?.Trim();
            sortBy = string.IsNullOrWhiteSpace(sortBy)
                ? "featured"
                : sortBy.Trim().ToLowerInvariant();

            // Lọc từ khóa.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                products = products.Where(x =>
                    x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            // Lọc danh mục.
            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(x =>
                    x.CategorySlug.Equals(
                        category,
                        StringComparison.OrdinalIgnoreCase));
            }

            // Lọc khoảng giá.
            if (minPrice.HasValue)
                products = products.Where(x => x.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                products = products.Where(x => x.Price <= maxPrice.Value);

            // Lọc đánh giá.
            if (rating.HasValue)
                products = products.Where(x => x.Rating >= rating.Value);

            // Lọc dung tích.
            if (!string.IsNullOrWhiteSpace(volume))
            {
                products = products.Where(x =>
                    x.Volume.Equals(volume, StringComparison.OrdinalIgnoreCase));
            }

            // Sắp xếp.
            products = sortBy switch
            {
                "latest" => products.OrderByDescending(x => x.Id),

                "price-asc" => products.OrderBy(x => x.Price),

                "price-desc" => products.OrderByDescending(x => x.Price),

                "rating" => products
                    .OrderByDescending(x => x.Rating)
                    .ThenByDescending(x => x.ReviewCount),

                "popular" => products.OrderByDescending(x => x.SoldCount),

                _ => products
                    .OrderByDescending(x => x.IsFeatured)
                    .ThenByDescending(x => x.SoldCount)
            };

            var filteredProducts = products.ToList();

            var totalProducts = filteredProducts.Count;

            var totalPages = (int)Math.Ceiling(
                totalProducts / (double)pageSize);

            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

            var pagedProducts = filteredProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Danh mục và dung tích lấy từ toàn bộ danh sách,
            // không lấy từ danh sách đã lọc.
            var categories = MockProductCatalog.Products
                .GroupBy(x => new { x.CategorySlug, x.CategoryName })
                .Select(x => new ShopCategoryFilterViewModel
                {
                    Slug = x.Key.CategorySlug,
                    Name = x.Key.CategoryName,
                    ProductCount = x.Count()
                })
                .ToList();

            var volumes = MockProductCatalog.Products
                .GroupBy(x => x.Volume)
                .Select(x => new ShopVolumeFilterViewModel
                {
                    Value = x.Key,
                    ProductCount = x.Count()
                })
                .OrderBy(x => x.Value)
                .ToList();

            return new ShopIndexViewModel
            {
                Products = pagedProducts,
                Categories = categories,
                Volumes = volumes,

                Keyword = keyword,
                Category = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Rating = rating,
                Volume = volume,
                SortBy = sortBy,

                CurrentPage = page,
                PageSize = pageSize,
                TotalProducts = totalProducts,

                AvailableMinPrice = MockProductCatalog.Products.Min(x => x.Price),
                AvailableMaxPrice = MockProductCatalog.Products.Max(x => x.Price)
            };
        }
    }
}
