using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Data.Context;
using Shared.Data.Entities.Media;
using Shared.DTOs.Banner;
using Shared.Enums.Banner;
using Shared.Interfaces.Banner;
using Shared.Interfaces.Media;
using Shared.Requests.Banner;
using Shared.Responses;
using Shared.Responses.Banner;
using Shared.Responses.Datatables;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using BannerEnity = Shared.Data.Entities.Marketing.Banner;

namespace Shared.Services.Banner
{
    public class BannerService : IBannerService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMediaService _mediaService;
        private readonly ILogger<BannerService> _logger;

        public BannerService(
    AppDbContext dbContext,
    IMediaService mediaService,
    ILogger<BannerService> logger)
        {
            _dbContext = dbContext;
            _mediaService = mediaService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<BannerResponse>> GetHomeBannersAsync(
    CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var banners = await _dbContext.Banners
                .AsNoTracking()
                .Where(x =>
                    x.Position == BannerPosition.HomeHero &&
                    x.IsPublished &&
                    !x.IsDeleted &&
                    (x.StartAt == null || x.StartAt <= now) &&
                    (x.EndAt == null || x.EndAt > now))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .Select(x => new BannerResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    LinkType = x.LinkType,
                    LinkValue = x.LinkValue,
                    Position = x.Position,
                    SortOrder = x.SortOrder,
                    IsPublished = x.IsPublished,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt,

                    Medias = x.BannerMedias
                        .OrderBy(m => m.SortOrder)
                        .Select(m => new BannerMediaResponse
                        {
                            Id = m.Id,
                            MediaType = m.MediaType,

                            DesktopMediaFileId = m.DesktopMediaFileId,
                            MobileMediaFileId = m.MobileMediaFileId,

                            DesktopUrl = m.DesktopMediaFile != null
                                ? "/uploads/" + m.DesktopMediaFile.StoragePath.Replace("\\", "/")
                                : null,

                            MobileUrl = m.MobileMediaFile != null
                                ? "/uploads/" + m.MobileMediaFile.StoragePath.Replace("\\", "/")
                                : null,

                            DesktopX = m.DesktopX,
                            DesktopY = m.DesktopY,
                            DesktopWidth = m.DesktopWidth,

                            MobileX = m.MobileX,
                            MobileY = m.MobileY,
                            MobileWidth = m.MobileWidth,

                            AltText = m.AltText,
                            SortOrder = m.SortOrder
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            // Không hiển thị Banner không có ảnh Background.
            return banners
                .Where(x => x.Medias.Any(m =>
                    m.MediaType == BannerMediaType.Background &&
                    (!string.IsNullOrWhiteSpace(m.DesktopUrl) ||
                     !string.IsNullOrWhiteSpace(m.MobileUrl))))
                .ToList();
        }
        public async Task<BannerDataTableResult> GetBannersAsync(
            BannerDataTableRequest request,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Tổng số Banner chưa áp dụng bộ lọc.
            var totalCount = await _dbContext.Banners.CountAsync(x => !x.IsDeleted, cancellationToken);

            var query = _dbContext.Banners.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

            // Tìm kiếm.
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var keyword = request.Search.Trim();

                query = query.Where(x =>
                    EF.Functions.ILike(x.Title, $"%{keyword}%") ||
                    (x.LinkValue != null &&
                     EF.Functions.ILike(x.LinkValue, $"%{keyword}%")));
            }

            // Lọc vị trí.
            if (request.Position.HasValue)
            {
                query = query.Where(x =>
                    x.Position == request.Position.Value);
            }

            // Lọc loại liên kết.
            if (request.LinkType.HasValue)
            {
                query = query.Where(x =>
                    x.LinkType == request.LinkType.Value);
            }

            // Lọc trạng thái.
            if (request.Status.HasValue)
            {
                query = ApplyStatusFilter(
                    query,
                    request.Status.Value,
                    now);
            }

            var filteredCount = await query.CountAsync(cancellationToken);

            // Sắp xếp.
            query = ApplySorting(
                query,
                request.SortColumn,
                request.SortDirection);

            // Giới hạn phân trang.
            var start = Math.Max(0, request.Start);
            var length = request.Length;

            if (length <= 0)
                length = 10;

            length = Math.Min(length, 100);

            // Truy vấn dữ liệu.
            var banners = await query
                .Skip(start)
                .Take(length)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Position,
                    x.LinkType,
                    x.StartAt,
                    x.EndAt,
                    x.IsPublished,
                    x.SortOrder,

                    Background = x.BannerMedias
                        .Where(m => m.MediaType == BannerMediaType.Background)
                        .Select(m => new
                        {
                            m.DesktopMediaFileId,
                            m.MobileMediaFileId,

                            DesktopImageUrl = m.DesktopMediaFile == null
                                ? null
                                : m.DesktopMediaFile.StoragePath == null
                                    ? null
                                    : m.DesktopMediaFile.StoragePath.StartsWith("/uploads/")
                                        ? m.DesktopMediaFile.StoragePath
                                        : "/uploads/" + m.DesktopMediaFile.StoragePath,

                            MobileImageUrl = m.MobileMediaFile == null
                                ? null
                                : m.MobileMediaFile.StoragePath == null
                                    ? null
                                    : m.MobileMediaFile.StoragePath.StartsWith("/uploads/")
                                        ? m.MobileMediaFile.StoragePath
                                        : "/uploads/" + m.MobileMediaFile.StoragePath
                        })
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            var items = banners.Select(x =>
            {
                var desktopId = x.Background?.DesktopMediaFileId;
                var mobileId = x.Background?.MobileMediaFileId;

                return new BannerDataTableItemDto
                {
                    Id = x.Id,
                    Title = x.Title,

                    Position = (int)x.Position,
                    PositionName = GetDisplayName(x.Position),

                    LinkType = (int)x.LinkType,

                    DesktopMediaFileId = desktopId,
                    MobileMediaFileId = mobileId,
                    DesktopImageUrl = x.Background?.DesktopImageUrl,
                    MobileImageUrl = x.Background?.MobileImageUrl,
                    HasDesktop = desktopId.HasValue,
                    HasMobile = mobileId.HasValue,

                    StartAt = x.StartAt,
                    EndAt = x.EndAt,
                    IsPublished = x.IsPublished,
                    Status = (int)GetStatus(
                        x.IsPublished,
                        x.StartAt,
                        x.EndAt,
                        now),

                    SortOrder = x.SortOrder
                };
            }).ToList();

            return new BannerDataTableResult
            {
                TotalCount = totalCount,
                FilteredCount = filteredCount,
                Items = items
            };
        }
        public async Task<(bool Success, string Message)> SetPublishStatusAsync(
    int id,
    bool isPublished,
    CancellationToken cancellationToken = default)
        {
            try
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var banner = await _dbContext.Banners
    .Include(x => x.BannerMedias)
        .ThenInclude(m => m.DesktopMediaFile)
    .Include(x => x.BannerMedias)
        .ThenInclude(m => m.MobileMediaFile)
    .FirstOrDefaultAsync(
        x => x.Id == id && !x.IsDeleted,
        cancellationToken);

                if (banner == null)
                {
                    return (false, "Không tìm thấy Banner hoặc Banner đã bị xóa.");
                }

                if (banner.IsPublished == isPublished)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return (true, "Trạng thái Banner không thay đổi.");
                }

                if (isPublished)
                {
                    var hasBackgroundImage = banner.BannerMedias.Any(m =>
    m.MediaType == BannerMediaType.Background &&
    (
        (m.DesktopMediaFileId.HasValue &&
         m.DesktopMediaFile != null) ||
        (m.MobileMediaFileId.HasValue &&
         m.MobileMediaFile != null)
    ));

                    if (!hasBackgroundImage)
                    {
                        return (false,
                            "Banner cần có ít nhất một ảnh Background hợp lệ trước khi xuất bản.");
                    }
                }

                if (!isPublished &&
                    banner.Position == BannerPosition.HomeHero &&
                    banner.IsPublished)
                {
                    var hasAnotherPublishedBanner =
                        await HasAnotherPublishedHomeHeroAsync(
                            banner.Id,
                            cancellationToken);

                    if (!hasAnotherPublishedBanner)
                    {
                        return (false,
                            "Không thể tắt Banner HomeHero cuối cùng đang xuất bản.");
                    }
                }

                banner.IsPublished = isPublished;
                banner.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return (
                    true,
                    isPublished
                        ? "Đã xuất bản Banner."
                        : "Đã tắt xuất bản Banner."
                );
            }
            catch (Exception ex) when (IsTransactionConflict(ex))
            {
                return (
                    false,
                    "Có thao tác khác đang cập nhật Banner cùng lúc. Vui lòng tải lại danh sách và thử lại."
                );
            }
        }

        public async Task<(bool Success, string Message)> DeleteBannerAsync(
     int id,
     CancellationToken cancellationToken = default)
        {
            try
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var banner = await _dbContext.Banners
                    .FirstOrDefaultAsync(
                        x => x.Id == id && !x.IsDeleted,
                        cancellationToken);

                if (banner == null)
                {
                    return (false, "Không tìm thấy Banner hoặc Banner đã được xóa.");
                }

                if (banner.Position == BannerPosition.HomeHero &&
                    banner.IsPublished)
                {
                    var hasAnotherPublishedBanner =
                        await HasAnotherPublishedHomeHeroAsync(
                            banner.Id,
                            cancellationToken);

                    if (!hasAnotherPublishedBanner)
                    {
                        return (false,
                            "Không thể xóa Banner HomeHero cuối cùng đang xuất bản.");
                    }
                }

                var now = DateTime.UtcNow;

                await RemoveSortOrderAsync(banner.Position, banner.SortOrder,
                    banner.Id, cancellationToken);

                banner.IsPublished = false;
                banner.IsDeleted = true;
                banner.DeletedAt = now;
                banner.UpdatedAt = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return (true, "Đã xóa Banner.");
            }
            catch (Exception ex) when (IsTransactionConflict(ex))
            {
                return (
                    false,
                    "Có thao tác khác đang cập nhật Banner cùng lúc. Vui lòng tải lại danh sách và thử lại."
                );
            }
        }

        private static IQueryable<BannerEnity> ApplyStatusFilter(
            IQueryable<BannerEnity> query,
            BannerStatus status,
            DateTime now)
        {
            return status switch
            {
                BannerStatus.Draft =>
                    query.Where(x => !x.IsPublished),

                BannerStatus.Scheduled =>
                    query.Where(x =>
                        x.IsPublished &&
                        x.StartAt.HasValue &&
                        x.StartAt.Value > now),

                BannerStatus.Expired =>
                    query.Where(x =>
                        x.IsPublished &&
                        (!x.StartAt.HasValue || x.StartAt.Value <= now) &&
                        x.EndAt.HasValue &&
                        x.EndAt.Value <= now),

                BannerStatus.Active =>
                    query.Where(x =>
                        x.IsPublished &&
                        (!x.StartAt.HasValue || x.StartAt.Value <= now) &&
                        (!x.EndAt.HasValue || x.EndAt.Value > now)),

                _ => query
            };
        }

        private static BannerStatus GetStatus(
            bool isPublished,
            DateTime? startAt,
            DateTime? endAt,
            DateTime now)
        {
            if (!isPublished)
                return BannerStatus.Draft;

            if (startAt.HasValue && startAt.Value > now)
                return BannerStatus.Scheduled;

            if (endAt.HasValue && endAt.Value <= now)
                return BannerStatus.Expired;

            return BannerStatus.Active;
        }

        private static IQueryable<BannerEnity> ApplySorting(
            IQueryable<BannerEnity> query,
            string? sortColumn,
            string? sortDirection)
        {
            var descending = string.Equals(
                sortDirection,
                "desc",
                StringComparison.OrdinalIgnoreCase);

            return sortColumn?.ToLowerInvariant() switch
            {
                "title" => descending
                    ? query.OrderByDescending(x => x.Title)
                    : query.OrderBy(x => x.Title),

                "position" => descending
                    ? query.OrderByDescending(x => x.Position)
                    : query.OrderBy(x => x.Position),

                "startat" => descending
                    ? query.OrderByDescending(x => x.StartAt)
                    : query.OrderBy(x => x.StartAt),

                "endat" => descending
                    ? query.OrderByDescending(x => x.EndAt)
                    : query.OrderBy(x => x.EndAt),

                "sortorder" => descending
                    ? query.OrderByDescending(x => x.SortOrder)
                    : query.OrderBy(x => x.SortOrder),

                "id" => descending
                    ? query.OrderByDescending(x => x.Id)
                    : query.OrderBy(x => x.Id),

                _ => query
                    .OrderBy(x => x.SortOrder)
                    .ThenByDescending(x => x.Id)
            };
        }

        private static string GetDisplayName(Enum value)
        {
            var member = value.GetType()
                .GetMember(value.ToString())
                .FirstOrDefault();

            var display = member?
                .GetCustomAttribute<DisplayAttribute>();

            return display?.GetShortName() ?? value.ToString();
        }
        private Task<bool> HasAnotherPublishedHomeHeroAsync(
     int currentBannerId,
     CancellationToken cancellationToken)
        {
            return _dbContext.Banners.AnyAsync(
                x =>
                    x.Id != currentBannerId &&
                    x.Position == BannerPosition.HomeHero &&
                    x.IsPublished &&
                    !x.IsDeleted,
                cancellationToken);
        }

        private static bool IsTransactionConflict(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is PostgresException postgresException &&
                    (postgresException.SqlState == PostgresErrorCodes.SerializationFailure ||
                     postgresException.SqlState == PostgresErrorCodes.DeadlockDetected))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<ServiceResult<int>> CreateAsync(
    BannerRequest request,
    CancellationToken cancellationToken = default)
        {
            var validationError = ValidateBannerRequest(request);

            if (validationError != null)
                return ServiceResult<int>.Fail(validationError);

            var uploadedFiles = new List<MediaFile>();
            var committed = false;

            try
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var now = DateTime.UtcNow;

                // Chừa vị trí cho Banner mới
                await InsertSortOrderAsync(
                    request.Position,
                    request.SortOrder,
                    cancellationToken);

                var banner = new BannerEnity
                {
                    Title = request.Title.Trim(),
                    LinkType = request.LinkType,
                    LinkValue = NormalizeLinkValue(request.LinkValue),
                    Position = request.Position,
                    SortOrder = request.SortOrder,
                    IsPublished = request.IsPublished,
                    StartAt = request.StartAt,
                    EndAt = request.EndAt,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false
                };

                foreach (var mediaRequest in request.Medias.OrderBy(x => x.SortOrder))
                {
                    if (mediaRequest.Id != 0)
                        return ServiceResult<int>.Fail(
                            "Không được gửi ID BannerMedia khi tạo Banner.");

                    if (mediaRequest.RemoveDesktopImage ||
                        mediaRequest.RemoveMobileImage)
                    {
                        return ServiceResult<int>.Fail(
                            "Không thể yêu cầu xóa ảnh khi tạo Banner.");
                    }

                    ValidateFileOperations(mediaRequest);

                    var media = new BannerMedia
                    {
                        MediaType = mediaRequest.MediaType,
                        DesktopX = mediaRequest.DesktopX,
                        DesktopY = mediaRequest.DesktopY,
                        DesktopWidth = mediaRequest.DesktopWidth,
                        MobileX = mediaRequest.MobileX,
                        MobileY = mediaRequest.MobileY,
                        MobileWidth = mediaRequest.MobileWidth,
                        AltText = mediaRequest.AltText?.Trim(),
                        SortOrder = mediaRequest.SortOrder
                    };

                    media.DesktopMediaFile = await UploadMediaAsync(
                        mediaRequest.DesktopFile,
                        uploadedFiles,
                        cancellationToken);

                    media.MobileMediaFile = await UploadMediaAsync(
                        mediaRequest.MobileFile,
                        uploadedFiles,
                        cancellationToken);

                    if (media.MediaType == BannerMediaType.Overlay &&
                        media.DesktopMediaFile == null &&
                        media.MobileMediaFile == null)
                    {
                        throw new InvalidOperationException(
                            "Overlay phải có ít nhất một ảnh Desktop hoặc Mobile.");
                    }

                    banner.BannerMedias.Add(media);
                }

                var backgroundCount = banner.BannerMedias.Count(
                    x => x.MediaType == BannerMediaType.Background);

                if (backgroundCount > 1)
                {
                    throw new InvalidOperationException(
                        "Mỗi Banner chỉ được có một Background.");
                }

                if (request.IsPublished && !HasValidBackground(banner))
                {
                    throw new InvalidOperationException(
                        "Banner cần có ít nhất một ảnh Background hợp lệ trước khi xuất bản.");
                }

                _dbContext.Banners.Add(banner);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                committed = true;

                return ServiceResult<int>.Success(
                    banner.Id,
                    "Tạo Banner thành công.");
            }
            catch (Exception ex) when (IsTransactionConflict(ex))
            {
                _logger.LogWarning(ex, "Banner creation transaction conflict.");

                return ServiceResult<int>.Fail(
                    "Có thao tác khác đang cập nhật Banner cùng lúc. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Banner.");

                return ServiceResult<int>.Fail(
                    ex is InvalidOperationException
                        ? ex.Message
                        : "Không thể tạo Banner. Vui lòng thử lại.");
            }
            finally
            {
                if (!committed)
                {
                    await CleanupUploadedFilesAsync(uploadedFiles);
                }
            }
        }

        public async Task<ServiceResult<BannerResponse>> GetUpdateBannerAsync(
    int id,
    CancellationToken cancellationToken = default)
        {
            if (id <= 0)
                return ServiceResult<BannerResponse>.Fail("Banner không hợp lệ.");

            var banner = await _dbContext.Banners
                .AsNoTracking()
                .Include(x => x.BannerMedias)
                    .ThenInclude(x => x.DesktopMediaFile)
                .Include(x => x.BannerMedias)
                    .ThenInclude(x => x.MobileMediaFile)
                .FirstOrDefaultAsync(
                    x => x.Id == id && !x.IsDeleted,
                    cancellationToken);

            if (banner == null)
            {
                return ServiceResult<BannerResponse>.Fail(
                    "Không tìm thấy Banner hoặc Banner đã bị xóa.");
            }

            var now = DateTime.UtcNow;

            var response = new BannerResponse
            {
                Id = banner.Id,
                Title = banner.Title,
                LinkType = banner.LinkType,
                LinkValue = banner.LinkValue,
                Position = banner.Position,
                SortOrder = banner.SortOrder,
                IsPublished = banner.IsPublished,
                StartAt = banner.StartAt,
                EndAt = banner.EndAt,

                Status = GetStatus(
                    banner.IsPublished,
                    banner.StartAt,
                    banner.EndAt,
                    now),

                Medias = banner.BannerMedias
                    .OrderBy(x => x.SortOrder)
                    .Select(x => new BannerMediaResponse
                    {
                        Id = x.Id,
                        MediaType = x.MediaType,

                        DesktopMediaFileId = x.DesktopMediaFileId,
                        DesktopUrl = BuildMediaUrl(x.DesktopMediaFile),

                        MobileMediaFileId = x.MobileMediaFileId,
                        MobileUrl = BuildMediaUrl(x.MobileMediaFile),

                        DesktopX = x.DesktopX,
                        DesktopY = x.DesktopY,
                        DesktopWidth = x.DesktopWidth,

                        MobileX = x.MobileX,
                        MobileY = x.MobileY,
                        MobileWidth = x.MobileWidth,

                        AltText = x.AltText,
                        SortOrder = x.SortOrder
                    })
                    .ToList()
            };
            return ServiceResult<BannerResponse>.Success(response, null);
        }

        public async Task<ServiceResult<int>> UpdateAsync(BannerRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Id <= 0)
                return ServiceResult<int>.Fail("Banner không hợp lệ.");

            var validationError = ValidateBannerRequest(request);

            if (validationError != null)
                return ServiceResult<int>.Fail(validationError);

            var uploadedFiles = new List<MediaFile>();
            var committed = false;

            try
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var banner = await _dbContext.Banners
                    .Include(x => x.BannerMedias)
                        .ThenInclude(x => x.DesktopMediaFile)
                    .Include(x => x.BannerMedias)
                        .ThenInclude(x => x.MobileMediaFile)
                    .FirstOrDefaultAsync(
                        x => x.Id == request.Id && !x.IsDeleted,
                        cancellationToken);

                if (banner == null)
                {
                    return ServiceResult<int>.Fail(
                        "Không tìm thấy Banner hoặc Banner đã bị xóa.");
                }

                // Kiểm tra quy tắc HomeHero khi rời khỏi trạng thái được bảo vệ.
                var wasPublishedHomeHero =
                    banner.Position == BannerPosition.HomeHero &&
                    banner.IsPublished;

                var willRemainPublishedHomeHero =
                    request.Position == BannerPosition.HomeHero &&
                    request.IsPublished;

                if (wasPublishedHomeHero && !willRemainPublishedHomeHero)
                {
                    var hasAnother = await HasAnotherPublishedHomeHeroAsync(
                        banner.Id,
                        cancellationToken);

                    if (!hasAnother)
                    {
                        return ServiceResult<int>.Fail(
                            "Không thể tắt hoặc chuyển vị trí Banner HomeHero cuối cùng đang xuất bản.");
                    }
                }

                var requestedIds = request.Medias
                    .Where(x => x.Id > 0)
                    .Select(x => x.Id)
                    .ToList();

                if (requestedIds.Count != requestedIds.Distinct().Count())
                {
                    return ServiceResult<int>.Fail(
                        "Danh sách BannerMedia chứa ID trùng lặp.");
                }

                var existingMedias = banner.BannerMedias.ToDictionary(x => x.Id);

                // Kiểm tra mọi ID gửi lên đều thuộc Banner đang sửa.
                if (requestedIds.Any(id => !existingMedias.ContainsKey(id)))
                {
                    return ServiceResult<int>.Fail(
                        "Có hình ảnh không thuộc Banner đang cập nhật.");
                }

                // Xóa những BannerMedia không còn trong Request.
                var removedMedias = banner.BannerMedias
                    .Where(x => !requestedIds.Contains(x.Id))
                    .ToList();

                foreach (var removedMedia in removedMedias)
                {
                    banner.BannerMedias.Remove(removedMedia);
                    _dbContext.BannerMedias.Remove(removedMedia);
                }

                foreach (var mediaRequest in request.Medias.OrderBy(x => x.SortOrder))
                {
                    ValidateFileOperations(mediaRequest);

                    BannerMedia media;

                    if (mediaRequest.Id > 0)
                    {
                        media = existingMedias[mediaRequest.Id];

                        media.MediaType = mediaRequest.MediaType;
                    }
                    else
                    {
                        media = new BannerMedia
                        {
                            BannerId = banner.Id,
                            MediaType = mediaRequest.MediaType
                        };

                        banner.BannerMedias.Add(media);
                    }

                    media.DesktopX = mediaRequest.DesktopX;
                    media.DesktopY = mediaRequest.DesktopY;
                    media.DesktopWidth = mediaRequest.DesktopWidth;

                    media.MobileX = mediaRequest.MobileX;
                    media.MobileY = mediaRequest.MobileY;
                    media.MobileWidth = mediaRequest.MobileWidth;

                    media.AltText = mediaRequest.AltText?.Trim();
                    media.SortOrder = mediaRequest.SortOrder;

                    // Desktop
                    if (mediaRequest.RemoveDesktopImage)
                    {
                        media.DesktopMediaFile = null;
                        media.DesktopMediaFileId = null;
                    }
                    else if (mediaRequest.DesktopFile is { Length: > 0 })
                    {
                        var newDesktopFile = await UploadMediaAsync(
                            mediaRequest.DesktopFile,
                            uploadedFiles,
                            cancellationToken);

                        media.DesktopMediaFile = newDesktopFile;
                    }

                    // Mobile
                    if (mediaRequest.RemoveMobileImage)
                    {
                        media.MobileMediaFile = null;
                        media.MobileMediaFileId = null;
                    }
                    else if (mediaRequest.MobileFile is { Length: > 0 })
                    {
                        var newMobileFile = await UploadMediaAsync(
                            mediaRequest.MobileFile,
                            uploadedFiles,
                            cancellationToken);

                        media.MobileMediaFile = newMobileFile;
                    }

                    if (media.MediaType == BannerMediaType.Overlay &&
                        media.DesktopMediaFile == null &&
                        media.MobileMediaFile == null)
                    {
                        throw new InvalidOperationException(
                            "Overlay phải có ít nhất một ảnh Desktop hoặc Mobile.");
                    }
                }

                var backgroundCount = banner.BannerMedias
                    .Count(x => x.MediaType == BannerMediaType.Background);

                if (backgroundCount > 1)
                {
                    throw new InvalidOperationException(
                        "Mỗi Banner chỉ được có một Background.");
                }

                if (request.IsPublished && !HasValidBackground(banner))
                {
                    throw new InvalidOperationException(
                        "Banner cần có ít nhất một ảnh Background hợp lệ trước khi xuất bản.");
                }
                await UpdateSortOrderAsync(banner,
    request.Position,
    request.SortOrder,
    cancellationToken);
                banner.Title = request.Title.Trim();
                banner.LinkType = request.LinkType;
                banner.LinkValue = NormalizeLinkValue(request.LinkValue);
                banner.Position = request.Position;
                banner.SortOrder = request.SortOrder;
                banner.IsPublished = request.IsPublished;
                banner.StartAt = request.StartAt;
                banner.EndAt = request.EndAt;
                banner.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                committed = true;

                return ServiceResult<int>.Success(
                    banner.Id,
                    "Cập nhật Banner thành công.");
            }
            catch (Exception ex) when (IsTransactionConflict(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Banner update transaction conflict. BannerId: {BannerId}",
                    request.Id);

                return ServiceResult<int>.Fail(
                    "Có thao tác khác đang cập nhật Banner cùng lúc. Vui lòng tải lại và thử lại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating Banner. BannerId: {BannerId}",
                    request.Id);

                return ServiceResult<int>.Fail(
                    ex is InvalidOperationException
                        ? ex.Message
                        : "Không thể cập nhật Banner. Vui lòng thử lại.");
            }
            finally
            {
                if (!committed)
                {
                    await CleanupUploadedFilesAsync(uploadedFiles);
                }
            }
        }

        private async Task<MediaFile?> UploadMediaAsync(
    IFormFile? file,
    List<MediaFile> uploadedFiles,
    CancellationToken cancellationToken)
        {
            if (file == null || file.Length <= 0)
                return null;

            var mediaFile = await _mediaService.UploadAsync(
                file,
                "Banner",
                cancellationToken);

            _dbContext.MediaFiles.Add(mediaFile);
            uploadedFiles.Add(mediaFile);

            return mediaFile;
        }

        private async Task CleanupUploadedFilesAsync(
    List<MediaFile> uploadedFiles)
        {
            foreach (var mediaFile in uploadedFiles)
            {
                try
                {
                    _dbContext.Entry(mediaFile).State = EntityState.Detached;

                    await _mediaService.DeleteAsync(mediaFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Không thể dọn file upload thất bại. StoragePath: {StoragePath}",
                        mediaFile.StoragePath);
                }
            }
        }

        private static bool HasValidBackground(BannerEnity banner)
        {
            return banner.BannerMedias.Any(m =>
                m.MediaType == BannerMediaType.Background &&
                (
                    m.DesktopMediaFile != null ||
                    m.MobileMediaFile != null
                ));
        }

        private static string? ValidateBannerRequest(BannerRequest request)
        {
            if (request == null)
                return "Dữ liệu Banner không hợp lệ.";

            if (string.IsNullOrWhiteSpace(request.Title))
                return "Vui lòng nhập tên Banner.";

            if (request.Title.Trim().Length > 200)
                return "Tên Banner không được vượt quá 200 ký tự.";

            if (!Enum.IsDefined(typeof(BannerPosition), request.Position))
                return "Vị trí Banner không hợp lệ.";

            if (!Enum.IsDefined(typeof(BannerLinkType), request.LinkType))
                return "Loại liên kết không hợp lệ.";

            if (request.StartAt.HasValue &&
                request.EndAt.HasValue &&
                request.EndAt.Value <= request.StartAt.Value)
            {
                return "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.";
            }

            if (request.Medias == null)
                return "Danh sách hình ảnh Banner không hợp lệ.";

            var backgroundCount = request.Medias.Count(
                x => x.MediaType == BannerMediaType.Background);

            if (backgroundCount > 1)
                return "Mỗi Banner chỉ được có một Background.";

            foreach (var media in request.Medias)
            {
                if (!Enum.IsDefined(typeof(BannerMediaType), media.MediaType))
                    return "Loại hình ảnh Banner không hợp lệ.";

                if (media.DesktopX < 0 || media.DesktopX > 100 ||
                    media.DesktopY < 0 || media.DesktopY > 100 ||
                    media.DesktopWidth < 0 || media.DesktopWidth > 100 ||
                    media.MobileX < 0 || media.MobileX > 100 ||
                    media.MobileY < 0 || media.MobileY > 100 ||
                    media.MobileWidth < 0 || media.MobileWidth > 100)
                {
                    return "Tọa độ và chiều rộng hình ảnh phải nằm trong khoảng 0 đến 100.";
                }

                if (media.SortOrder < 0)
                    return "Thứ tự hình ảnh không hợp lệ.";
            }

            return null;
        }

        private static void ValidateFileOperations(
    BannerMediaRequest request)
        {
            if (request.RemoveDesktopImage &&
                request.DesktopFile is { Length: > 0 })
            {
                throw new InvalidOperationException(
                    "Không thể vừa xóa vừa upload ảnh Desktop.");
            }

            if (request.RemoveMobileImage &&
                request.MobileFile is { Length: > 0 })
            {
                throw new InvalidOperationException(
                    "Không thể vừa xóa vừa upload ảnh Mobile.");
            }
        }

        private static string? NormalizeLinkValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? BuildMediaUrl(MediaFile? mediaFile)
        {
            if (mediaFile == null ||
                string.IsNullOrWhiteSpace(mediaFile.StoragePath))
            {
                return null;
            }

            var path = mediaFile.StoragePath.Replace('\\', '/');

            return path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                ? path
                : "/uploads/" + path.TrimStart('/');
        }

        private async Task InsertSortOrderAsync(
    BannerPosition position,
    int sortOrder,
    CancellationToken cancellationToken)
        {
            var banners = await _dbContext.Banners
                .Where(x =>
                    x.Position == position &&
                    !x.IsDeleted &&
                    x.SortOrder >= sortOrder)
                .ToListAsync(cancellationToken);

            foreach (var banner in banners)
            {
                banner.SortOrder++;
            }
        }

        private async Task UpdateSortOrderAsync(
    BannerEnity banner,
    BannerPosition newPosition,
    int newSortOrder,
    CancellationToken cancellationToken)
        {
            var oldPosition = banner.Position;
            var oldSortOrder = banner.SortOrder;

            // Không thay đổi vị trí
            if (oldPosition == newPosition)
            {
                if (oldSortOrder == newSortOrder)
                    return;

                var samePositionBanners = await _dbContext.Banners
                    .Where(x =>
                        x.Id != banner.Id &&
                        x.Position == oldPosition &&
                        !x.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var item in samePositionBanners)
                {
                    if (newSortOrder < oldSortOrder &&
                        item.SortOrder >= newSortOrder &&
                        item.SortOrder < oldSortOrder)
                    {
                        item.SortOrder++;
                    }
                    else if (newSortOrder > oldSortOrder &&
                             item.SortOrder > oldSortOrder &&
                             item.SortOrder <= newSortOrder)
                    {
                        item.SortOrder--;
                    }
                }

                return;
            }

            // Đóng khoảng trống ở vị trí cũ
            var oldPositionBanners = await _dbContext.Banners
                .Where(x =>
                    x.Id != banner.Id &&
                    x.Position == oldPosition &&
                    !x.IsDeleted &&
                    x.SortOrder > oldSortOrder)
                .ToListAsync(cancellationToken);

            foreach (var item in oldPositionBanners)
                item.SortOrder--;

            // Mở vị trí mới
            var newPositionBanners = await _dbContext.Banners
                .Where(x =>
                    x.Id != banner.Id &&
                    x.Position == newPosition &&
                    !x.IsDeleted &&
                    x.SortOrder >= newSortOrder)
                .ToListAsync(cancellationToken);

            foreach (var item in newPositionBanners)
                item.SortOrder++;
        }


        private async Task RemoveSortOrderAsync(
    BannerPosition position,
    int sortOrder,
    int bannerId,
    CancellationToken cancellationToken)
        {
            var banners = await _dbContext.Banners
                .Where(x =>
                    x.Id != bannerId &&
                    x.Position == position &&
                    !x.IsDeleted &&
                    x.SortOrder > sortOrder)
                .ToListAsync(cancellationToken);

            foreach (var banner in banners)
            {
                banner.SortOrder--;
            }
        }
    }
}
