namespace Shared.Responses.Product
{
    public class ProductVariantResponse
    {
        public int Id { get; set; }

        public Dictionary<string, string> Options { get; set; } = [];

        public string SKU { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }
    }
}
