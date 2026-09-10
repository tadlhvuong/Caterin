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

        public MediaService(IWebHostEnvironment environment, IOptions<MediaStorageOptions> options)
        {
            _environment = environment;
            _options = options.Value;
        }

        public async Task<MediaFile> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Chỉ hỗ trợ JPG, JPEG, PNG hoặc WEBP.");
            }

            var safeFolder = folder.Replace('\\', '/').Trim('/');

            var physicalFolder = Path.Combine(_environment.ContentRootPath, _options.RootPath,
                    safeFolder.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(physicalFolder);

            var generatedFileName = $"{Guid.NewGuid():N}{extension}";

            var physicalPath = Path.Combine(physicalFolder, generatedFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, 81920,  true))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var relativePath = $"{safeFolder}/{generatedFileName}";

            return new MediaFile
            {
                FileName = generatedFileName,

                OriginalFileName = Path.GetFileName(file.FileName),

                StoragePath = relativePath,

                ContentType = file.ContentType,

                Size = file.Length,

                CreatedAt = DateTime.UtcNow
            };
        }
        public Task DeleteAsync(MediaFile mediaFile, CancellationToken cancellationToken = default)
        {
            if (mediaFile == null)
            {
                return Task.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(
                    mediaFile.StoragePath))
            {
                return Task.CompletedTask;
            }

            var physicalPath = Path.Combine(_environment.ContentRootPath, mediaFile.StoragePath
                        .Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            return Task.CompletedTask;
        }
        public Task DeleteFolderAsync(string relativeFolder, CancellationToken cancellationToken = default)
        {
            var safeFolder = relativeFolder.Replace('\\', '/').Trim('/');

            var physicalFolder = Path.Combine(_environment.ContentRootPath, _options.RootPath,
                    safeFolder.Replace('/', Path.DirectorySeparatorChar));

            if (Directory.Exists(physicalFolder))
            {
                Directory.Delete(physicalFolder, recursive: true);
            }

            return Task.CompletedTask;
        }
    }
}
