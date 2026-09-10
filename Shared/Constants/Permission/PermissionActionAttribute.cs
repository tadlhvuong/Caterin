using Shared.Enums;

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
