using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website.Models.Shop;
using Website.Models.Wishlist;

namespace Website.Controllers
{
    [AllowAnonymous]
    [Route("wishlist")]
    public class WishlistController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View(new WishlistIndexViewModel());
        }

        /// <summary>
        /// Lấy thông tin sản phẩm theo danh sách ID trong localStorage.
        /// Ví dụ: /wishlist/items?ids=1,2,3
        /// </summary>
        [HttpGet("items")]
        public IActionResult GetItems([FromQuery] string? ids)
        {
            Response.Headers.CacheControl = "no-store";

            var productIds = ParseProductIds(ids);

            if (productIds.Count == 0)
                return Ok(Array.Empty<object>());

            var products = MockProductCatalog.GetByIds(productIds);

            var result = products.Select(product => new
            {
                id = product.Id,
                slug = product.Slug,
                name = product.Name,
                categoryName = product.CategoryName,
                imageUrl = Url.Content(product.ImageUrl),
                price = product.Price,
                originalPrice = product.OriginalPrice,
                rating = product.Rating,
                reviewCount = product.ReviewCount,
                volume = product.Volume,
                stockQuantity = product.StockQuantity,
                isAvailable = product.StockQuantity > 0
            });

            return Ok(result);
        }

        private static List<int> ParseProductIds(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return [];

            return ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .Take(100)
                .ToList();
        }
    }
}
