using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Constants.Permission
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PermissionModuleAttribute : Attribute
    {
        public string Module { get; }

        public PermissionModuleAttribute(string module)
        {
            Module = module;
        }
    }
}
