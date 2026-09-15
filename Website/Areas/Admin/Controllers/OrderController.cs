using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Enums;
using Shared.Interfaces.Core;
using Shared.Requests.Order;
using Shared.Responses.Datatables;
using Shared.Services.Product;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

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
        public async Task<IActionResult> GetProducts([FromBody] OrderDataTableRequest request)
        {
            var result = await _orderService.GetOrdersAsync(request);

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                data = result.Items
            });
        }

        [HttpGet("order-statuses")]
        [PermissionAction(ActionType.View)]
        public IActionResult GetOrderStatuses()
        {
            var statuses = Enum.GetValues<OrderStatus>()
                .Select(x =>
                {
                    var orderStatus = typeof(OrderStatus)
                        .GetMember(x.ToString())
                        .First();

                    var display = orderStatus.GetCustomAttribute<DisplayAttribute>();

                    return new
                    {
                        value = (int)x,
                        name = display?.GetShortName() ?? x.ToString()
                    };
                });

            return Ok(statuses);
        }

        [HttpGet("payment-statuses")]
        [PermissionAction(ActionType.View)]
        public IActionResult GetPaymentStatuses()
        {
            var statuses = Enum.GetValues<PaymentStatus>()
                .Select(x =>
                {
                    var paymentStatus = typeof(PaymentStatus)
                        .GetMember(x.ToString())
                        .First();

                    var display = paymentStatus.GetCustomAttribute<DisplayAttribute>();

                    return new
                    {
                        value = (int)x,
                        name = display?.GetShortName() ?? x.ToString()
                    };
                });

            return Ok(statuses);
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
