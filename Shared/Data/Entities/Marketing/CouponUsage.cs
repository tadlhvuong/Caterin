namespace Shared.Data.Entities.Marketing
{
    public class CouponUsage
    {
        public int Id { get; set; }
        public int CouponId { get; set; }
        public string UserId { get; set; }
        public int OrderId { get; set; }
        public DateTime UsedAt { get; set; }
    }
}
