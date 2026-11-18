
namespace Website.Models.Shop
{
    public class ShopCategoryFilterViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }

    public class ShopVolumeFilterViewModel
    {
        public string Value { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}
