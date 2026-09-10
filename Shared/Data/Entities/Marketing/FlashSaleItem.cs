namespace Shared.Data.Entities.Marketing
{
    public class FlashSaleItem
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int ProductVariantId {get;set;}
        public double SalePrice { get; set; }
        public int QuantityLimit { get; set;}
        public int SoldQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
