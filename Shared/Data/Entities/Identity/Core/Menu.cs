using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class Menu
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Url { get; set; } = null!;

        public string Icon { get; set; } = null!;

        public int SortOrder { get; set; }

        public int? ParentId { get; set; }

        public int? PermissionId { get; set; }

        public bool IsActive { get; set; }

        public virtual Menu? Parent { get; set; }

        public virtual ICollection<Menu> Children { get; set; }
            = new HashSet<Menu>();

        public virtual Permission? Permission { get; set; }
    }
}
