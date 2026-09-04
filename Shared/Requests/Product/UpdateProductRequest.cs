using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Product
{
    public class UpdateProductRequest
    {
        /// <summary>
        /// Tên sản phẩm
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Slug sản phẩm
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        /// Mô tả ngắn
        /// </summary>
        public string? ShortDescription { get; set; }

        /// <summary>
        /// Mô tả chi tiết
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Danh mục
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Giá bán
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Giá niêm yết / giá gốc
        /// </summary>
        public decimal? CompareAtPrice { get; set; }

        /// <summary>
        /// Đơn vị tính
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Khối lượng
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Sản phẩm nổi bật
        /// </summary>
        public bool IsFeatured { get; set; }

        /// <summary>
        /// SEO title
        /// </summary>
        public string? MetaTitle { get; set; }

        /// <summary>
        /// SEO description
        /// </summary>
        public string? MetaDescription { get; set; }
    }
}
