namespace Shared.Services.Caches
{
    public sealed class CacheSettings
    {
        public int PermissionCodeLookupMinutes { get; set; } = 30;

        public int UserPermissionSnapshotMinutes { get; set; } = 30;
    }
}
