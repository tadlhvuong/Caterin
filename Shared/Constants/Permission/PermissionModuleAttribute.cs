namespace Shared.Constants.Permission
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PermissionModuleAttribute : Attribute
    {
        public string Module { get; }

        public PermissionModuleAttribute(string module)
        {
            if (string.IsNullOrWhiteSpace(module))
                throw new ArgumentException(
                    "Module cannot be empty.",
                    nameof(module));

            Module = module.Trim();
        }
    }
}
