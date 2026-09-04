namespace Website.Areas.Admin.Models.Product
{
    public class ProductVariantImageViewModel
    {
        public long Id { get; set; }

        public string Key { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}
