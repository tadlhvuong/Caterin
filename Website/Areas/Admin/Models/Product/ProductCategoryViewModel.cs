using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.Requests.Product.Category;

namespace Website.Areas.Admin.Models.Product
{
    public class ProductCategoryViewModel
    {
        public List<SelectListItem> Statuses { get; set; } = [];
        public EditProductCategoryRequest CreateProduct { get; set; }
    }
}
