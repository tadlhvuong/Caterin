namespace Shared.Services.Email.EmailModels
{
    public class CustomerCareEmailModel : BaseEmailModel
    {
        // Customer
        public string CustomerName { get; set; }


        // Review
        public string ProductName { get; set; }

        public string OrderCode { get; set; }

        public string ReviewUrl { get; set; }


        // Voucher
        public string VoucherCode { get; set; }

        public string VoucherName { get; set; }

        public string VoucherValue { get; set; }

        public decimal MinimumOrderAmount { get; set; }

        public DateTime? VoucherExpireDate { get; set; }

        public string VoucherUrl { get; set; }


        // Shopping
        public string ShoppingUrl { get; set; }


        // Loyalty
        public int EarnedPoints { get; set; }

        public int CurrentPoints { get; set; }

        public string PointReason { get; set; }

        public string RewardUrl { get; set; }
    }
}
