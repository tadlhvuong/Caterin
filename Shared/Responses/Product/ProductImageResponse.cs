namespace Shared.Responses.Product
{
    public class ProductImageResponse
    {
        public int Id { get; set; }

        public string Url { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; }
    }
}
