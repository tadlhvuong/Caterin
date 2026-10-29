using Microsoft.AspNetCore.Http;
using Shared.Data.Entities.Media;

namespace Shared.Interfaces.Media
{
    public interface IMediaService
    {
        /// <summary>
        /// Tải file theo chính sách upload được cấu hình.
        /// Ví dụ: Product, Chat, Avatar.
        /// </summary>
        Task<MediaFile> UploadAsync(
            IFormFile file,
            string policyName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa file vật lý.
        /// </summary>
        Task DeleteAsync(
            MediaFile mediaFile,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa thư mục vật lý.
        /// </summary>
        Task DeleteFolderAsync(
            string relativeFolder,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mở file để đọc.
        /// </summary>
        Task<FileStream?> OpenReadAsync(
            MediaFile mediaFile,
            CancellationToken cancellationToken = default);
    }
}
