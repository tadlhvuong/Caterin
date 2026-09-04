using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Order;
using Shared.Enums;
using Shared.Interfaces.Core;
using Shared.Requests;
using Shared.Requests.Order;
using Website.Areas.Admin.Models.Order;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/order")]
    [PermissionModule("Order")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<OrderController> _logger;

        private readonly IOrderService _orderService;
        public OrderController(AppDbContext dbContext, ILogger<OrderController> logger, IOrderService orderService) 
        {
            _dbContext = dbContext;
            _logger = logger;
            _orderService = orderService;
        }
        [PermissionAction(ActionType.View)]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [PermissionAction(ActionType.View)]
        [HttpPost("get-orders")]
        public async Task<IActionResult> Data([FromBody] OrderDataTableRequest request, CancellationToken cancellationToken)
        {
            var pageSize = request.Length <= 0 ? 20 : request.Length;

            var page = request.Start / pageSize + 1;

            var sort = request.Order.FirstOrDefault();

            var sortColumn = sort?.Column switch
            {
                0 => "OrderCode",
                1 => "Status",
                2 => "TotalAmount",
                3 => "ItemCount",
                4 => "CreatedAt",
                _ => "CreatedAt"
            };

            var serviceRequest = new OrderListRequest
            {
                Page = page,
                PageSize = pageSize,
                Search = request.Search?.Value,
                Status = request.Status,
                SortColumn = sortColumn,
                SortDescending = sort?.Dir == "desc"
            };


            var result = await _orderService.GetOrdersAsync(serviceRequest, cancellationToken);

            if (!result.Succeeded)
            {
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = Array.Empty<object>(),
                    errors = result.Errors
                });
            }

            var data = result.Data!;


            return Json(new
            {
                draw = request.Draw,
                recordsTotal = data.TotalCount,
                recordsFiltered = data.FilteredCount,
                data = data.Items
            });
        }
        [HttpGet("order-details/{id?}")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _orderService.GetDetailsAsync(id);

            if (result == null)
                return NotFound();

            return View(result);
        }
    }
}
