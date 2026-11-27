using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Entities.Marketing;
using Shared.Enums;
using Shared.Enums.Banner;
using Shared.Interfaces.Banner;
using Shared.Requests.Banner;
using Shared.Responses.Datatables;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/banner")]
    [PermissionModule("Banner")]
    public class BannerController : Controller
    {
        private readonly IBannerService _bannerService;

        public BannerController(IBannerService bannerService)
        {
            _bannerService = bannerService;
        }

        [HttpGet("")]
        [PermissionAction(ActionType.View)]
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost("get-banners")]
        [PermissionAction(ActionType.View)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetBanners([FromBody] BannerDataTableRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _bannerService.GetBannersAsync(
                request,
                cancellationToken);

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                data = result.Items
            });
        }

        [HttpGet("statuses")]
        [PermissionAction(ActionType.View)]
        public IActionResult GetStatuses()
        {
            return Ok(GetEnumOptions<BannerStatus>());
        }

        [HttpGet("positions")]
        [PermissionAction(ActionType.View)]
        public IActionResult GetPositions()
        {
            return Ok(GetEnumOptions<BannerPosition>());
        }

        [HttpGet("link-types")]
        [PermissionAction(ActionType.View)]
        public IActionResult GetLinkTypes()
        {
            return Ok(GetEnumOptions<BannerLinkType>());
        }

        private static IEnumerable<object> GetEnumOptions<T>()
            where T : struct, Enum
        {
            return Enum.GetValues<T>().Select(value =>
            {
                var member = typeof(T).GetMember(value.ToString())
                    .FirstOrDefault();

                var display = member?
                    .GetCustomAttribute<DisplayAttribute>();

                return new
                {
                    value = Convert.ToInt32(value),
                    name = display?.GetShortName() ?? value.ToString()
                };
            });
        }

        [HttpPost("toggle-publish")]
        [PermissionAction(ActionType.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(
    [FromBody] BannerPublishRequest request,
    CancellationToken cancellationToken)
        {
            if (request.Id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Banner không hợp lệ."
                });
            }

            var result = await _bannerService.SetPublishStatusAsync(
                request.Id,
                request.IsPublished,
                cancellationToken);

            return Json(new
            {
                success = result.Success,
                message = result.Message
            });
        }

        [HttpGet("create")]
        [PermissionAction(ActionType.Create)]
        public IActionResult Create()
        {
            ViewData["BannerId"] = 0;
            return View("CreateEdit");
        }

        [HttpPost("create")]
        [PermissionAction(ActionType.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [FromForm] BannerRequest request,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Dữ liệu Banner không hợp lệ.",
                    errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => new
                        {
                            field = x.Key,
                            message = e.ErrorMessage
                        }))
                });
            }

            var result = await _bannerService.CreateAsync(request, cancellationToken);

            if (!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    message = result.Message ?? "Không thể tạo Banner.",
                    errors = result.Errors
                });
            }

            return Json(new
            {
                success = true,
                message = result.Message ?? "Tạo Banner thành công.",
                data = result.Data,
                redirectUrl = Url.Action("Index", "Banner", new { area = "Admin" })
            });
        }

        [HttpGet("edit/{id:int}")]
        [PermissionAction(ActionType.Edit)]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            ViewData["BannerId"] = id;
            return View("CreateEdit");
        }

        [HttpGet("get-update-banner")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> GetUpdateBanner(
    [FromQuery] int id,
    CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Banner không hợp lệ."
                });
            }

            var result = await _bannerService.GetUpdateBannerAsync(
                id,
                cancellationToken);

            if (!result.Succeeded)
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Errors.FirstOrDefault()?.Message
                        ?? "Không tìm thấy Banner."
                });
            }

            return Json(new
            {
                success = true,
                data = result.Data
            });
        }

        [HttpPost("edit")]
        [PermissionAction(ActionType.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    [FromForm] BannerRequest request,
    CancellationToken cancellationToken)
        {
            if (request.Id <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Banner không hợp lệ."
                });
            }

            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Dữ liệu Banner không hợp lệ.",
                    errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => new
                        {
                            field = x.Key,
                            message = e.ErrorMessage
                        }))
                });
            }

            var result = await _bannerService.UpdateAsync(
                request,
                cancellationToken);

            if (!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    message = result.Message ?? "Không thể cập nhật Banner.",
                    errors = result.Errors
                });
            }

            return Json(new
            {
                success = true,
                message = result.Message ?? "Cập nhật Banner thành công.",
                data = result.Data,
                redirectUrl = Url.Action("Index", "Banner", new { area = "Admin" })
            });
        }

        [HttpPost("delete")]
        [PermissionAction(ActionType.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            [FromBody] BannerDeleteRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Banner không hợp lệ."
                });
            }

            var result = await _bannerService.DeleteBannerAsync(
                request.Id,
                cancellationToken);

            return Json(new
            {
                success = result.Success,
                message = result.Message
            });
        }
    }
}
