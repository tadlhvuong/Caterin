using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Product
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string SKU { get; set; }
        public string ShortDescription { get; set; }
        public string MetaTilte { get; set; }
        public string MetaDescription { get; set; }
        public string Thumbnail { get; set; }
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }  
        public DateTime UpdatedAt { get; set; }
    }
    
}
