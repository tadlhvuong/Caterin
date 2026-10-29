namespace Shared.DTOs.Identity
{
    public class MediaStorageOptions
    {
        public string RootPath { get; set; } = "wwwroot/uploads";

        public Dictionary<string, MediaUploadPolicy> Policies { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
    }

    public class MediaUploadPolicy
    {
        public string Folder { get; set; } = string.Empty;

        public HashSet<string> AllowedExtensions { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public long MaxFileSize { get; set; }
    }
}
