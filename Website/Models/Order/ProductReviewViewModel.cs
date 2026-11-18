
using System.ComponentModel.DataAnnotations;

namespace Website.Models.Order
{
    public class ProductReviewViewModel
    {
        public string OrderCode { get; set; } = string.Empty;

        public DateTime OrderCreatedAt { get; set; }

        public List<ProductReviewItemViewModel> Items { get; set; } = [];
    }

    public class ProductReviewItemViewModel
    {
        public int ItemIndex { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? VariantName { get; set; }

        public string? ImageUrl { get; set; }

        public int PurchasedQuantity { get; set; }

        public bool IsReviewed { get; set; }

        [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao đánh giá.")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Nội dung đánh giá không được vượt quá 1.000 ký tự.")]
        public string? Comment { get; set; }
    }
}
