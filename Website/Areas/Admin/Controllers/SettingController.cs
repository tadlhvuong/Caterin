using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Log;
using Website.Areas.Admin.Models;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/setting")]
    [PermissionModule("Setting")]
    public class SettingController : Controller
    {
        private readonly AppDbContext _dbContext;

        public SettingController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [PermissionAction(ActionType.View)]
        [HttpGet("")]
        public IActionResult Index(string tab="general")
        {
            var filter = _dbContext.Settings.Where(x => x.Group == SettingGroup.General).ToList();
            if (filter == null) return NotFound();
            var result = new SettingViewModel();
            foreach(var s in filter)
            {
                var setting = new SettingResult
                {
                    Key = ParseKey(s.Key).InputId,
                    Value = s.Value ?? "",
                };
                result.Settings.Add(setting);
            }    
            var AllowedTabs = new List<string> { "general", "checkout", "payment", "delivery", "location", "notification" };
            if (!AllowedTabs.Contains(tab))
            {
                return NotFound();
            }
            result.CurrentTab = tab;
            return View(result);
        }

        private static string GenerateKey(string group, string inputId)
        {
            if (string.IsNullOrWhiteSpace(group))
                throw new ArgumentException("Group is required.", nameof(group));

            if (string.IsNullOrWhiteSpace(inputId))
                throw new ArgumentException("InputId is required.", nameof(inputId));

            return $"{Normalize(group)}_{Normalize(inputId)}";
        }

        private static string Normalize(string value)
        {
            return value
                .Trim()
                .Replace("-", "_")
                .ToLowerInvariant();
        }
        private static (string Group, string InputId) ParseKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            var index = key.IndexOf('_');

            if (index <= 0 || index >= key.Length - 1)
                throw new FormatException($"Invalid setting key: {key}");

            var group = key[..index];
            var inputId = key[(index + 1)..];

            return (group, inputId);
        }

        [HttpGet("general")]
        [PermissionAction(ActionType.View)]
        public IActionResult General()
        {
            try
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                    return PartialView("_General");

                return RedirectToAction(nameof(Index), new { tab = "general" });
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }

        [PermissionAction(ActionType.View)]
        [HttpGet("checkout")]
        public IActionResult Checkout()
        {
            try
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                    return PartialView("_Checkout");

                return RedirectToAction(nameof(Index), new { tab = "checkout" });
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }
        [PermissionAction(ActionType.View)]
        [HttpGet("payment")]
        public IActionResult Payment()
        {
            try
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                    return PartialView("_Payment");

                return RedirectToAction(nameof(Index), new { tab = "payment" });
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }
        [PermissionAction(ActionType.View)]
        [HttpGet("delivery")]
        public IActionResult Delivery()
        {
            try
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                    return PartialView("_Delivery");

                return RedirectToAction(nameof(Index), new { tab = "delivery" });
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }
        [PermissionAction(ActionType.View)]
        [HttpGet("location")]
        public IActionResult Location()
        {
            try
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                    return PartialView("_Location");

                return RedirectToAction(nameof(Index), new { tab = "location" });
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }
        [PermissionAction(ActionType.View)]
        [HttpGet("notification")]
        public IActionResult Notification()
        {
            try
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                    return PartialView("_Notification");

                return RedirectToAction(nameof(Index), new { tab = "notification" });
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }
    }
}
