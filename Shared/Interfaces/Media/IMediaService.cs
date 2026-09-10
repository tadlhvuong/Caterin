using Microsoft.AspNetCore.Http;
using Shared.Data.Entities.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Media
{
    public interface IMediaService
    {
        /// <summary>
        /// Tải file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="folder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MediaFile> UploadAsync(IFormFile file,string folder, CancellationToken cancellationToken = default);
        /// <summary>
        /// Xóa file
        /// </summary>
        /// <param name="mediaFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);
        /// <summary>
        /// Xóa file vật lí
        /// </summary>
        /// <param name="relativeFolder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteFolderAsync(string relativeFolder, CancellationToken cancellationToken = default);
    }
}
