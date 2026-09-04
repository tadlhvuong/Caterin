using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Product
{
    public class ProductImageRequest
    {
        [Required]
        public IFormFile File { get; set; } = default!;

        [StringLength(250)]
        public string? AltText { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; }
    }
}
