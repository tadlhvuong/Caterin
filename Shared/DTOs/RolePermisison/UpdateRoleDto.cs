using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.RolePermisison
{
    public class UpdateRoleDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Level { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
