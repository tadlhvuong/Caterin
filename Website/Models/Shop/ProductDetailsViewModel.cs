namespace Website.Models.Shop
{
    public class ProductDetailsViewModel
    {
        public ShopProductViewModel Product { get; set; } = new();

        public List<ShopProductViewModel> RelatedProducts { get; set; } = [];
    }
}
