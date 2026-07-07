using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Identity
{
    public class MenuDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public int PermissionId { get; set; }

        public List<MenuDto> Children { get; set; } = new();
    }
}
