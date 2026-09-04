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
        Task<MediaFile> UploadAsync(
            IFormFile file,
            string folder,
            CancellationToken cancellationToken = default);
        Task DeleteAsync(
      MediaFile mediaFile,
      CancellationToken cancellationToken = default);
        Task DeleteFolderAsync(
            string relativeFolder,
            CancellationToken cancellationToken = default);
    }
}
