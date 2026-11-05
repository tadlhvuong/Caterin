namespace Shared.Helpers
{
    public sealed class AccessDeniedException : Exception
    {
        public AccessDeniedException()
            : base("Bạn không có quyền thực hiện thao tác này.")
        {
        }

        public AccessDeniedException(string message)
            : base(message)
        {
        }
    }
}
