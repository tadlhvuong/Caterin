namespace Shared.Requests.Product
{
    public class ProductVariantRequest
    {
        public int? Id { get; set; }

        public Dictionary<string, string> Options { get; set; } = [];

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string SKU { get; set; }
    }
}
