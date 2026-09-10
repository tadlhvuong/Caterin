namespace Shared.Data.Entities.Product
{
    public class ProductTagMapping
    {
        public int ProductId { get; set; }

        public int TagId { get; set; }

        public int DisplayOrder { get; set; }

        public Product Product { get; set; } = null!;

        public ProductTag Tag { get; set; } = null!;
    }
}
