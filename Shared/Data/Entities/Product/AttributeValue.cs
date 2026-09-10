namespace Shared.Data.Entities.Product
{
    public class AttributeValue
    {
        public int Id { get; set; }
        public int AttributeId { get; set; }
        public string Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public Attribute Attribute { get; set; } = null!;
        public ICollection<VariantAttribute> VariantAttributes { get; set; } = [];
        public ICollection<ProductVariantMedia> ProductVariantMedias { get; set; } = [];
    }
}
