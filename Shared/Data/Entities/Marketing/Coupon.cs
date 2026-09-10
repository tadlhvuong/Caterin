namespace Shared.Data.Entities.Marketing
{
    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double DiscountValue { get; set; }
        public bool IsPercentage { get; set; }
        public double MaxDiscountAmount { get; set; }
        public double MinimumOrderAmount { get; set; }
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
