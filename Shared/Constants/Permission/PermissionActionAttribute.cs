using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Constants.Permission
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PermissionActionAttribute : Attribute
    {
        public ActionType Action { get; }

        public PermissionActionAttribute(ActionType action)
        {
            Action = action;
        }
    }
}
