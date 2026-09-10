using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Core;
using Shared.Interfaces.Log;
using Shared.Responses.Datatables;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Website.Areas.Admin.Models;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/customer")]
    [PermissionModule("Customer")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;

        private readonly ILogger<CustomerController> _logger;
        private readonly IActivityLogger _activityLogger;

        public CustomerController(AppDbContext dbContext, ICustomerService customerService,  IUserService userService,
            ILogger<CustomerController> logger, IActivityLogger activityLogger)
        {
            _logger = logger;
            _activityLogger = activityLogger;
            _customerService = customerService;
            _userService = userService;

            _dbContext = dbContext;
        }

        [HttpGet("")]
        [PermissionAction(ActionType.View)]
        public ActionResult Index()
        {
            var user = _dbContext.Users.ToList();
            return View(user);
        }

        [HttpPost("get-customers")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetCustomers([FromBody] CustomerDataTableRequest request)
        {
            var result = await _customerService.GetCustomersAsync(request);

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
            var statuses = Enum.GetValues<EntityStatus>()
                .Select(x =>
                {
                    var member = typeof(EntityStatus).GetMember(x.ToString()).First();

                    var display = member.GetCustomAttribute<DisplayAttribute>();

                    return new
                    {
                        value = (int)x,
                        name = display?.GetShortName() ?? x.ToString()
                    };
                });

            return Ok(statuses);
        }
        [HttpGet("customer-details/{id?}")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Details(string id, string tab = "overview")
        {
            var user = await _userService.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new UserDetailsViewModel
            {
                CurrentTab = tab,
                Id = id,
                UserName = user.UserName!,
                Email = user.Email!,
                ConfirmEmail = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber!,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLogin,
            };
            var AllowedTabs = new List<string> { "overview", "security", "address", "invoice", "notification" };
            if (!AllowedTabs.Contains(tab))
            {
                return NotFound();
            }
            return View(vm);
        }
    }
}
