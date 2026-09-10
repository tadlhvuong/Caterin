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
