namespace Website.Areas.Admin.Models.Order
{
    public class OrderItemViewModel
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }= string.Empty;
        public string ProductVariant {  get; set; }
        public decimal? Price { get; set; }
        public int Quanitty { get; set; }
        public decimal? Total { get; set; }
    }
}
