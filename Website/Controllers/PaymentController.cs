
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using Website.Models.Order;
using Website.Models.Payment;

namespace Website.Controllers
{
    [Route("payment")]
    public class PaymentController : Controller
    {
        /// <summary>
        /// Trang thông báo kết quả thanh toán.
        /// Chưa xác minh giao dịch ở giai đoạn này.
        /// </summary>

        [AllowAnonymous]
        [HttpGet("result")]
        public IActionResult Result(string? status, string? orderCode)
        {
            // Giữ tương thích với trang kết quả mock cũ.
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                var defaultModel = status?.ToLowerInvariant() switch
                {
                    "success" => GetMockPaymentSuccess(),
                    "failed" => GetMockPaymentFailed(),
                    "pending" => GetMockPaymentPending(),
                    _ => GetMockPaymentPending()
                };

                return View(defaultModel);
            }

            var order = OrderController.GetMockMyOrders()
                .Orders
                .FirstOrDefault(x =>
                    x.OrderCode.Equals(
                        orderCode,
                        StringComparison.OrdinalIgnoreCase));

            if (order == null)
                return NotFound();

            var model = status?.ToLowerInvariant() switch
            {
                "success" => GetMockPaymentSuccess(),
                "failed" => GetMockPaymentFailed(),
                "pending" => GetMockPaymentPending(),
                _ => GetMockPaymentPending()
            };

            model.OrderCode = order.OrderCode;
            model.Amount = order.TotalAmount;
            model.PaymentMethod = order.PaymentMethod;
            model.ReturnUrl = Url.Action(
                "Details",
                "Order",
                new { orderCode = order.OrderCode }) ?? "/";

            return View(model);
        }

        private static PaymentResultViewModel GetMockPaymentSuccess()
        {
            return new PaymentResultViewModel
            {
                OrderCode = "CTN-20261111-0001",
                TransactionCode = "TXN202611110001",
                Result = "Success",
                Title = "Thanh toán thành công!",
                Message = "Giao dịch của bạn đã được xác nhận. Caterin sẽ sớm chuẩn bị và giao đơn hàng đến bạn.",
                PaymentMethod = "VNPay",
                Amount = 258000m,
                PaidAt = new DateTime(2026, 11, 11, 14, 45, 30),
                ReturnUrl = "/order/details/CTN-20261111-0001"
            };
        }

        private static PaymentResultViewModel GetMockPaymentFailed()
        {
            return new PaymentResultViewModel
            {
                OrderCode = "CTN-20261111-0001",
                TransactionCode = "TXN202611110002",
                Result = "Failed",
                Title = "Thanh toán không thành công",
                Message = "Giao dịch chưa được hoàn tất. Vui lòng thử lại hoặc chọn phương thức thanh toán khác.",
                PaymentMethod = "VNPay",
                Amount = 258000m,
                PaidAt = null,
                ReturnUrl = "/order/details/CTN-20261111-0001"
            };
        }

        private static PaymentResultViewModel GetMockPaymentPending()
        {
            return new PaymentResultViewModel
            {
                OrderCode = "CTN-20261111-0001",
                TransactionCode = "TXN202611110003",
                Result = "Pending",
                Title = "Giao dịch đang được xử lý",
                Message = "Chúng tôi đang chờ xác nhận từ ngân hàng hoặc cổng thanh toán. Vui lòng không thực hiện thanh toán lại cho đến khi trạng thái giao dịch được cập nhật.",
                PaymentMethod = "VNPay",
                Amount = 258000m,
                PaidAt = null,
                ReturnUrl = "/order/details/CTN-20261111-0001"
            };
        }

        // =========================
        // RETRY PAYMENT
        // =========================

        [Authorize]
        [HttpGet("retry/{orderCode}")]
        public IActionResult Retry(string orderCode)
        {
            var model = BuildRetryPaymentViewModel(orderCode);

            if (model == null)
                return NotFound();
            if (model.PaymentMethod != "VNPay")
            {
                TempData["ErrorMessage"] =
                    "Đơn hàng này không sử dụng phương thức thanh toán trực tuyến.";

                return RedirectToAction(
                    "Details",
                    "Order",
                    new { orderCode });
            }
            if (model.PaymentStatus == "Paid")
            {
                return RedirectToAction(
                    "Details",
                    "Order",
                    new { orderCode });
            }

            if (model.OrderStatus == "Cancelled")
            {
                TempData["ErrorMessage"] =
                    "Đơn hàng đã bị hủy, không thể thanh toán.";

                return RedirectToAction(
                    "Details",
                    "Order",
                    new { orderCode });
            }

            if (model.PaymentStatus == "Pending")
            {
                TempData["InfoMessage"] =
                    "Giao dịch thanh toán đang được xử lý. Vui lòng kiểm tra lại sau.";

                return RedirectToAction(
                    "Details",
                    "Order",
                    new { orderCode });
            }

            if (model.PaymentStatus != "Unpaid" &&
                model.PaymentStatus != "Failed")
            {
                return BadRequest();
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("retry/{orderCode}")]
        [ValidateAntiForgeryToken]
        public IActionResult Retry(
            string orderCode,
            RetryPaymentViewModel model)
        {
            var currentModel = BuildRetryPaymentViewModel(orderCode);

            if (currentModel == null)
                return NotFound();
            if (currentModel.PaymentMethod != "VNPay")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng này không sử dụng phương thức thanh toán trực tuyến."
                });
            }
            if (currentModel.OrderStatus == "Cancelled")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng đã bị hủy, không thể thanh toán."
                });
            }

            if (currentModel.PaymentStatus == "Paid")
            {
                return Conflict(new
                {
                    success = false,
                    message = "Đơn hàng này đã được thanh toán."
                });
            }

            if (currentModel.PaymentStatus == "Pending")
            {
                return Conflict(new
                {
                    success = false,
                    message = "Giao dịch đang được xử lý. Vui lòng không tạo thêm yêu cầu thanh toán."
                });
            }

            if (currentModel.PaymentStatus != "Unpaid" &&
                currentModel.PaymentStatus != "Failed")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Trạng thái thanh toán không hợp lệ."
                });
            }

            if (model.SelectedPaymentMethod != "VNPay")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Phương thức thanh toán không hợp lệ."
                });
            }

            // Mock: tạo yêu cầu thanh toán.
            // Chỉ chuyển sang Pending, chưa xác nhận giao dịch là Paid.
            var added = OrderController.MockPaymentStates.TryAdd(
                currentModel.OrderCode,
                "Pending");

            if (!added)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Yêu cầu thanh toán đang được xử lý. Vui lòng kiểm tra lại sau."
                });
            }

            return Json(new
            {
                success = true,
                message = "Yêu cầu thanh toán đã được tạo.",
                redirectUrl = Url.Action(
                    nameof(Result),
                    "Payment",
                    new
                    {
                        orderCode = currentModel.OrderCode,
                        status = "pending"
                    })
            });
        }

        // =========================
        // BUILD VIEW MODEL
        // =========================

        private RetryPaymentViewModel? BuildRetryPaymentViewModel(
    string orderCode)
        {
            var order = OrderController.GetMockMyOrders()
                .Orders
                .FirstOrDefault(x =>
                    x.OrderCode.Equals(
                        orderCode,
                        StringComparison.OrdinalIgnoreCase));

            if (order == null)
                return null;

            // Lấy trạng thái thanh toán mới nhất từ mock state.
            var paymentStatus = OrderController.MockPaymentStates.TryGetValue(
                order.OrderCode,
                out var mockStatus)
                    ? mockStatus
                    : order.PaymentStatus;

            var paymentStatusLabel = paymentStatus switch
            {
                "Unpaid" => "Chưa thanh toán",
                "Pending" => "Đang xử lý",
                "Paid" => "Đã thanh toán",
                "Failed" => "Thanh toán thất bại",
                _ => order.PaymentStatusLabel
            };

            var methods = new List<RetryPaymentMethodViewModel>
    {
        new()
        {
            Code = "VNPay",
            Name = "VNPay",
            Description = "Thanh toán trực tuyến qua VNPay.",
            Icon = "ph ph-credit-card"
        }
    };

            return new RetryPaymentViewModel
            {
                OrderCode = order.OrderCode,
                OrderCreatedAt = order.CreatedAt,
                OrderStatus = order.Status,
                OrderStatusLabel = order.StatusLabel,
                TotalAmount = order.TotalAmount,

                PaymentStatus = paymentStatus,
                PaymentStatusLabel = paymentStatusLabel,

                PaymentMethod = order.PaymentMethod,
                PaymentMethodLabel = order.PaymentMethodLabel,

                SelectedPaymentMethod = "VNPay",
                PaymentMethods = methods
            };
        }

    }
}
