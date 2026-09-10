namespace Shared.Requests.Product
{
    public class ProductOptionRequest
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Values { get; set; } = [];
    }
}
