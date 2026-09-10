using Microsoft.AspNetCore.Http;

namespace Shared.Requests.Product
{
    public class ProductVariantImageRequest
    {
        public string Key { get; set; } = string.Empty;

        public long? Id { get; set; }

        public IFormFile? File { get; set; } = null!;
    }
}
