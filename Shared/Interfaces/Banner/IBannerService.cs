using Microsoft.EntityFrameworkCore;
using Shared.Enums.Banner;
using Shared.Requests.Banner;
using Shared.Responses;
using Shared.Responses.Banner;
using Shared.Responses.Datatables;

namespace Shared.Interfaces.Banner
{
    public interface IBannerService
    {
        Task<IReadOnlyList<BannerResponse>> GetHomeBannersAsync(
    CancellationToken cancellationToken = default);
        Task<BannerDataTableResult> GetBannersAsync(
            BannerDataTableRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> CreateAsync(
        BannerRequest request,
        CancellationToken cancellationToken = default);

        Task<ServiceResult<BannerResponse>> GetUpdateBannerAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> UpdateAsync(
            BannerRequest request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string Message)> SetPublishStatusAsync(
            int id,
            bool isPublished,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string Message)> DeleteBannerAsync(
            int id,
            CancellationToken cancellationToken = default);
    }
}
