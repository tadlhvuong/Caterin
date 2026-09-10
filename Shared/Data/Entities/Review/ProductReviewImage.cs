namespace Shared.Data.Entities.Review
{
    public class ProductReviewImage
    {
        public int Id { get; set; }
        public int productReviewId { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
