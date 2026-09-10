using Microsoft.AspNetCore.Http;

namespace Shared.Requests.Product
{
    public class ProductImageRequest
    {
        public int? Id { get; set; }

        public IFormFile? File { get; set; } = default!;

        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; }
    }
}
