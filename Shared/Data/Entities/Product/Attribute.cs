namespace Shared.Data.Entities.Product
{
    public class Attribute
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime CreatedAt {  get; set; }
        public ICollection<AttributeValue> Values { get; set; } = [];
    }
}
