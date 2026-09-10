namespace Shared.Data.Entities.Product
{
    public class VariantAttribute
    {
        public int ProductVariantId { get; set; }
        public int AttributeValueId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        public AttributeValue AttributeValue { get; set; } = null!;
    }
}
