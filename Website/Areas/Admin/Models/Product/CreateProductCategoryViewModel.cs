using Microsoft.AspNetCore.Mvc.Rendering;

namespace Website.Areas.Admin.Models.Product
{
    public class CreateProductCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string? Description { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeyword { get; set; }
        public bool IsActive { get; set; }
    }
}
