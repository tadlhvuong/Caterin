using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shared.Data.Entities.Media;
using Shared.DTOs.Identity;
using Shared.Interfaces.Media;

namespace Shared.Services.Media
{

    public class MediaService : IMediaService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly MediaStorageOptions _options;

        private static readonly HashSet<string> ProductExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            ".jpg", ".jpeg", ".png", ".webp"
            };

        private static readonly HashSet<string> ChatExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            // Images
            ".jpg", ".jpeg", ".png", ".webp",

            // Documents
            ".pdf", ".doc", ".docx",
            ".xls", ".xlsx",
            ".ppt", ".pptx",
            ".txt", ".csv"
            };

        private const long MaxProductFileSize = 5 * 1024 * 1024;
        private const long MaxChatFileSize = 10 * 1024 * 1024;

        public MediaService(
            IWebHostEnvironment environment,
            IOptions<MediaStorageOptions> options)
        {
            _environment = environment;
            _options = options.Value;
        }

        public async Task<MediaFile> UploadAsync(
            IFormFile file,
            string folder,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length <= 0)
                throw new ArgumentException("File không hợp lệ.");

            var safeFolder = NormalizeFolder(folder);

            // Chỉ cho phép những thư mục upload đã định nghĩa.
            var isProduct = string.Equals(
                safeFolder, "products", StringComparison.OrdinalIgnoreCase);

            var isChat = string.Equals(
                safeFolder, "chat", StringComparison.OrdinalIgnoreCase);

            if (!isProduct && !isChat)
                throw new InvalidOperationException("Thư mục upload không hợp lệ.");

            var extension = Path.GetExtension(file.FileName);

            var allowedExtensions = isProduct
                ? ProductExtensions
                : ChatExtensions;

            var maxFileSize = isProduct
                ? MaxProductFileSize
                : MaxChatFileSize;

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    "Định dạng file không được hỗ trợ.");

            if (file.Length > maxFileSize)
                throw new InvalidOperationException(
                    $"File không được vượt quá {maxFileSize / (1024 * 1024)} MB.");

            ValidateContentType(extension, file.ContentType);

            var physicalFolder = GetPhysicalPath(safeFolder);
            Directory.CreateDirectory(physicalFolder);

            var generatedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var physicalPath = Path.Combine(physicalFolder, generatedFileName);

            try
            {
                await using var stream = new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);

                await file.CopyToAsync(stream, cancellationToken);
            }
            catch
            {
                if (File.Exists(physicalPath))
                    File.Delete(physicalPath);

                throw;
            }

            return new MediaFile
            {
                FileName = generatedFileName,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoragePath = $"{safeFolder}/{generatedFileName}",
                ContentType = file.ContentType,
                Size = file.Length,
                CreatedAt = DateTime.UtcNow
            };
        }

        public Task DeleteAsync(
            MediaFile mediaFile,
            CancellationToken cancellationToken = default)
        {
            if (mediaFile == null ||
                string.IsNullOrWhiteSpace(mediaFile.StoragePath))
            {
                return Task.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var physicalPath = GetPhysicalPath(mediaFile.StoragePath);

            if (File.Exists(physicalPath))
                File.Delete(physicalPath);

            return Task.CompletedTask;
        }

        public Task DeleteFolderAsync(
            string relativeFolder,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var physicalFolder = GetPhysicalPath(relativeFolder);

            if (Directory.Exists(physicalFolder))
                Directory.Delete(physicalFolder, recursive: true);

            return Task.CompletedTask;
        }

        private string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("Thư mục không hợp lệ.");

            var normalized = folder.Replace('\\', '/').Trim('/');

            // Không chấp nhận đường dẫn con hoặc traversal.
            if (normalized.Contains('/') ||
                normalized == "." ||
                normalized == "..")
            {
                throw new InvalidOperationException(
                    "Thư mục upload không hợp lệ.");
            }

            return normalized;
        }

        private string GetPhysicalPath(string relativePath)
        {
            var root = Path.GetFullPath(
                Path.Combine(_environment.ContentRootPath, _options.RootPath));

            var normalized = relativePath.Replace(
                '/', Path.DirectorySeparatorChar);

            var fullPath = Path.GetFullPath(
                Path.Combine(root, normalized));

            var rootWithSeparator =
                root.TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                    rootWithSeparator,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Đường dẫn file không hợp lệ.");
            }

            return fullPath;
        }

        private static void ValidateContentType(
            string extension,
            string contentType)
        {
            var allowedTypes = extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => new[] { "image/jpeg" },
                ".png" => new[] { "image/png" },
                ".webp" => new[] { "image/webp" },
                ".pdf" => new[] { "application/pdf" },
                ".doc" => new[] { "application/msword" },
                ".docx" => new[]
                {
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            },
                ".xls" => new[] { "application/vnd.ms-excel" },
                ".xlsx" => new[]
                {
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            },
                ".ppt" => new[] { "application/vnd.ms-powerpoint" },
                ".pptx" => new[]
                {
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            },
                ".txt" => new[] { "text/plain" },
                ".csv" => new[] { "text/csv", "application/csv", "application/vnd.ms-excel" },
                _ => Array.Empty<string>()
            };

            if (!allowedTypes.Contains(
                    contentType ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Content-Type không phù hợp với định dạng file.");
            }
        }

        public async Task<FileStream?> OpenReadAsync(MediaFile mediaFile, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediaFile);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(mediaFile.StoragePath))
                return null;

            var rootPath = Path.GetFullPath(
                Path.Combine(_environment.ContentRootPath, _options.RootPath));

            var relativePath = mediaFile.StoragePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var physicalPath = Path.GetFullPath(
                Path.Combine(rootPath, relativePath));

            var rootWithSeparator =
                rootPath.TrimEnd(Path.DirectorySeparatorChar,
                                 Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!physicalPath.StartsWith(rootWithSeparator, comparison))
                throw new InvalidOperationException("Đường dẫn file không hợp lệ.");

            if (!System.IO.File.Exists(physicalPath))
                return null;

            return new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);
        }
    }
}
