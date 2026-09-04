using System.ComponentModel.DataAnnotations;

namespace Website.Areas.Admin.Models.Product
{
    public sealed class ProductOptionViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public List<string> Values { get; set; } = [];
    }
}
