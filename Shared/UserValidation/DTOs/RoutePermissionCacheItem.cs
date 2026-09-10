namespace Shared.UserValidation.DTOs
{
    public sealed class RoutePermissionCacheItem
    {
        public int PermissionId { get; init; }

        public string PermissionCode { get; init; } = "";
        public string Route { get; init; } = "";

        public string HttpMethod { get; init; } = "";
    }
}
