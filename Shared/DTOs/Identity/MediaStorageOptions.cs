using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Identity
{
    public class MediaStorageOptions
    {
        public string RootPath { get; set; } = "wwwroot/uploads";

        public string ProductFolder { get; set; } = "products";
    }
}
