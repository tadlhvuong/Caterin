using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Website.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost("place-order")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder()
        {
            // TODO:
            // 1. Validate thông tin Checkout.
            // 2. Kiểm tra giỏ hàng.
            // 3. Tạo Order và OrderItems.
            // 4. Xử lý phương thức thanh toán.

            // Tạm thời chưa triển khai nghiệp vụ.
            return RedirectToAction("Success", "Order", new {  orderCode = "" });
        }
    }
}
