using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Product.Category
{
    public class CreateProductCategoryRequest
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Category Slug is required.")]
        [StringLength(100, ErrorMessage = "Category Slug cannot exceed 100 characters.")]
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public bool IsActive { get; set; }
    }
    public class EditProductCategoryRequest
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Category Slug is required.")]
        [StringLength(100, ErrorMessage = "Category Slug cannot exceed 100 characters.")]
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public bool IsActive { get; set; }
    }
}
