namespace Website.Areas.Admin.Models.Product
{
    public class ProductImageViewModel
    {
        public long Id { get; set; }

        public string Url { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; }
    }
}
