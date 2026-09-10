using Shared.Enums;

namespace Shared.Data.Entities.Payment
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int PaymentMethodId { get; set; }
        public EntityStatus Status { get; set; }
        public double Amount { get; set; }
        public string TransactionCode { get; set; }
        public DateTime PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
