using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Data.Entities.Order;
using System.Collections.Concurrent;
using Website.Models.Order;

namespace Website.Controllers
{
    [AllowAnonymous]
    [Route("order")]
    public class OrderController : Controller
    {

        [Authorize]
        [HttpGet("")]
        public IActionResult Index(string? keyword, string? status, int page = 1)
        {
            var model = BuildMyOrdersViewModel(keyword, status, page);

            return View(model);
        }

        [Authorize]
        [HttpGet("get-orders")]
        public IActionResult GetOrders(string? keyword, string? status, int page = 1)
        {
            var model = BuildMyOrdersViewModel(keyword, status, page);

            return PartialView("Partials/_OrderList", model);
        }



        private MyOrdersViewModel BuildMyOrdersViewModel(
            string? keyword,
            string? status,
            int page = 1)
        {
            const int pageSize = 10;

            var data = GetMockMyOrders();

            // Đồng bộ trạng thái thanh toán, hủy và đánh giá.
            foreach (var order in data.Orders)
            {
                // Đồng bộ trạng thái thanh toán.
                if (MockPaymentStates.TryGetValue(
                        order.OrderCode,
                        out var mockPaymentStatus))
                {
                    order.PaymentStatus = mockPaymentStatus;

                    order.PaymentStatusLabel = mockPaymentStatus switch
                    {
                        "Unpaid" => "Chưa thanh toán",
                        "Pending" => "Đang xử lý",
                        "Paid" => "Đã thanh toán",
                        "Failed" => "Thanh toán thất bại",
                        _ => order.PaymentStatusLabel
                    };
                }
                if (CancelledOrders.ContainsKey(order.OrderCode))
                {
                    order.Status = "Cancelled";
                    order.StatusLabel = "Đã hủy";
                    order.StatusColor = "danger";
                    order.CanCancel = false;
                    order.CanReview = false;
                    continue;
                }

                // Chỉ đơn hoàn thành mới được đánh giá.
                if (order.Status != "Completed")
                {
                    order.ReviewedItemCount = 0;
                    order.CanReview = false;
                    continue;
                }

                // Đếm số dòng sản phẩm đã đánh giá.
                order.ReviewedItemCount = ProductReviews.Keys.Count(key =>
                    key.StartsWith(
                        $"{order.OrderCode}:",
                        StringComparison.OrdinalIgnoreCase));

                order.CanReview =
                    order.ReviewedItemCount < order.ReviewableItemCount;
            }

            // Chuẩn hóa từ khóa và trạng thái.
            keyword = keyword?.Trim();

            status = string.IsNullOrWhiteSpace(status) ||
                     status.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? "All"
                : status;

            // Tìm kiếm theo mã đơn hàng.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                data.Orders = data.Orders
                    .Where(x => x.OrderCode.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Thống kê theo kết quả tìm kiếm.
            var pendingOrders = data.Orders.Count(x =>
                x.Status == "Pending");

            var inProgressOrders = data.Orders.Count(x =>
                x.Status == "Processing" ||
                x.Status == "Shipping");

            var completedOrders = data.Orders.Count(x =>
                x.Status == "Completed");

            // Lọc trạng thái.
            var filteredOrders = data.Orders;

            if (status != "All")
            {
                filteredOrders = filteredOrders
                    .Where(x => x.Status.Equals(
                        status,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var totalOrders = filteredOrders.Count;

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalOrders / (double)pageSize));

            page = Math.Clamp(page, 1, totalPages);

            return new MyOrdersViewModel
            {
                Keyword = keyword,
                StatusFilter = status,
                CurrentPage = page,
                PageSize = pageSize,

                TotalOrders = totalOrders,
                TotalPages = totalPages,

                PendingOrders = pendingOrders,
                InProgressOrders = inProgressOrders,
                CompletedOrders = completedOrders,

                Orders = filteredOrders
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList()
            };
        }



        /// <summary>
        /// Trang xác nhận đặt hàng thành công.
        /// </summary>
        [HttpGet("success")]
        public IActionResult Success(string? orderCode)
        {
            var model = GetMockOrder();

            if (!string.IsNullOrWhiteSpace(orderCode))
                model.OrderCode = orderCode;

            return View(model);
        }

        /// <summary>
        /// Trang chi tiết đơn hàng.
        /// Tạm thời chỉ hiển thị giao diện.
        /// </summary>
        [HttpGet("details/{orderCode}")]
        public IActionResult Details(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return NotFound();

            var order = GetMockMyOrders().Orders.FirstOrDefault(x =>
                string.Equals(
                    x.OrderCode,
                    orderCode,
                    StringComparison.OrdinalIgnoreCase));

            if (order == null)
                return NotFound();

            var model = GetMockOrder();
            model.OrderCode = orderCode;

            // Đặt lại trạng thái timeline trước khi áp dụng trạng thái đặc biệt.
            foreach (var item in model.Timeline)
            {
                item.IsCurrent = false;
            }

            // Đơn đã hủy.
            if (CancelledOrders.TryGetValue(orderCode, out var cancellation))
            {
                model.Status = "Cancelled";
                model.StatusLabel = "Đã hủy";
                model.StatusColor = "danger";

                model.CanCancel = false;
                model.CanReview = false;
                model.CanRequestReturn = false;

                model.Timeline.Add(new OrderTimelineItemViewModel
                {
                    Title = "Đơn hàng đã bị hủy",
                    Description = $"Lý do: {cancellation.Reason}",
                    OccurredAt = cancellation.CancelledAt,
                    IsCompleted = true,
                    IsCurrent = true,
                    Icon = "ph-x-circle"
                });

                return View(model);
            }

            // Đồng bộ trạng thái đánh giá sản phẩm.
            if (model.Status == "Completed" &&
                !CancelledOrders.ContainsKey(model.OrderCode))
            {
                var reviewedCount = ProductReviews.Keys.Count(key =>
                    key.StartsWith(
                        $"{model.OrderCode}:",
                        StringComparison.OrdinalIgnoreCase));

                model.ReviewedItemCount = reviewedCount;
                model.ReviewableItemCount = model.Items.Count;
                model.CanReview = reviewedCount < model.ReviewableItemCount;
            }
            else
            {
                model.ReviewedItemCount = 0;
                model.ReviewableItemCount = 0;
                model.CanReview = false;
            }

            // Đồng bộ trạng thái đơn hàng.
            model.Status = order.Status;
            model.StatusLabel = order.StatusLabel;
            model.StatusColor = order.StatusColor;
            model.CanCancel = order.CanCancel;

            // Đồng bộ phương thức thanh toán.
            model.PaymentMethod = order.PaymentMethod;
            model.PaymentMethodLabel = order.PaymentMethodLabel;

            // Đồng bộ trạng thái thanh toán mới nhất.
            if (MockPaymentStates.TryGetValue(order.OrderCode, out var paymentStatus))
            {
                order.PaymentStatus = paymentStatus;
                order.PaymentStatusLabel = paymentStatus switch
                {
                    "Unpaid" => "Chưa thanh toán",
                    "Pending" => "Đang xử lý",
                    "Paid" => "Đã thanh toán",
                    "Failed" => "Thanh toán thất bại",
                    _ => order.PaymentStatusLabel
                };
            }

            model.PaymentStatus = order.PaymentStatus;
            model.PaymentStatusLabel = order.PaymentStatusLabel;

            var reviewModel = BuildProductReviewViewModel(orderCode);

            if (reviewModel != null)
            {
                model.CanReview = reviewModel.Items.Any(x => !x.IsReviewed);
            }

            // Đơn đã có yêu cầu đổi trả.
            if (ReturnRequests.TryGetValue(orderCode, out var request))
            {
                model.CanRequestReturn = false;

                model.ReturnRequest = new ReturnRequestSummaryViewModel
                {
                    RequestCode = request.RequestCode,
                    RequestType = request.RequestType,
                    RequestTypeLabel = request.RequestType == "Refund"
                        ? "Hoàn tiền"
                        : "Đổi sản phẩm",
                    Reason = request.Reason,
                    Description = request.Description,
                    Status = request.Status,
                    StatusLabel = request.StatusLabel,
                    StatusColor = request.StatusColor,
                    RequestedAt = request.RequestedAt
                };
            }

            return View(model);
        }
        [Authorize]
        [HttpGet("{orderCode}/invoice")]
        public IActionResult Invoice(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return NotFound();

            var order = GetMockMyOrders().Orders.FirstOrDefault(x =>
                string.Equals(
                    x.OrderCode,
                    orderCode,
                    StringComparison.OrdinalIgnoreCase));

            if (order == null)
                return NotFound();

            // Không phát hành hóa đơn cho đơn đã hủy.
            if (CancelledOrders.ContainsKey(order.OrderCode))
                return RedirectToAction(
                    nameof(Details),
                    new { orderCode = order.OrderCode });

            var model = GetMockOrder();

            // Thông tin đơn hàng.
            model.OrderCode = order.OrderCode;
            model.CreatedAt = order.CreatedAt;
            model.Status = order.Status;
            model.StatusLabel = order.StatusLabel;
            model.StatusColor = order.StatusColor;

            // Phương thức thanh toán.
            model.PaymentMethod = order.PaymentMethod;
            model.PaymentMethodLabel = order.PaymentMethodLabel;

            // Trạng thái thanh toán mới nhất.
            if (MockPaymentStates.TryGetValue(
                order.OrderCode,
                out var paymentStatus))
            {
                order.PaymentStatus = paymentStatus;
                order.PaymentStatusLabel = paymentStatus switch
                {
                    "Unpaid" => "Chưa thanh toán",
                    "Pending" => "Đang xử lý",
                    "Paid" => "Đã thanh toán",
                    "Failed" => "Thanh toán thất bại",
                    _ => order.PaymentStatusLabel
                };
            }

            model.PaymentStatus = order.PaymentStatus;
            model.PaymentStatusLabel = order.PaymentStatusLabel;

            return View(model);
        }
        /// <summary>
        /// Trang tra cứu và theo dõi đơn hàng.
        /// </summary>
        [HttpGet("track")]
        public IActionResult Track(string? orderCode)
        {
            var model = GetMockTracking();

            if (!string.IsNullOrWhiteSpace(orderCode))
                model.OrderCode = orderCode;

            return View(model);
        }

        [HttpPost("{orderCode}/cancel")]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(
    string orderCode,
    [FromBody] CancelOrderViewModel model)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Mã đơn hàng không hợp lệ."
                });
            }

            if (model == null || string.IsNullOrWhiteSpace(model.Reason))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng chọn lý do hủy đơn hàng."
                });
            }

            var allowedReasons = new[]
            {
        "Tôi muốn thay đổi sản phẩm.",
        "Tôi muốn thay đổi địa chỉ giao hàng.",
        "Tôi muốn thay đổi phương thức thanh toán.",
        "Tôi tìm được sản phẩm tốt hơn.",
        "Tôi không còn nhu cầu mua hàng.",
        "Lý do khác."
    };

            if (!allowedReasons.Contains(model.Reason))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lý do hủy đơn hàng không hợp lệ."
                });
            }

            var reason = model.Reason;

            if (model.Reason == "Lý do khác.")
            {
                if (string.IsNullOrWhiteSpace(model.OtherReason))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng nhập lý do hủy đơn hàng."
                    });
                }

                reason = model.OtherReason.Trim();

                if (reason.Length > 500)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Lý do hủy không được vượt quá 500 ký tự."
                    });
                }
            }

            // Tìm đơn hàng trong danh sách mock.
            var mockOrders = GetMockMyOrders();

            var order = mockOrders.Orders.FirstOrDefault(x =>
                string.Equals(
                    x.OrderCode,
                    orderCode,
                    StringComparison.OrdinalIgnoreCase));
            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy đơn hàng."
                });
            }

            // Không cho phép hủy đơn đã hủy.
            if (CancelledOrders.ContainsKey(orderCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng này đã được hủy trước đó."
                });
            }

            // Chỉ cho phép hủy khi đang chờ xác nhận hoặc đã xác nhận.
            if (order.Status is not ("Pending" or "Confirmed"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng không thể hủy ở trạng thái hiện tại."
                });
            }

            var cancellationInfo = new MockCancellationInfo
            {
                Reason = reason,
                CancelledAt = DateTime.Now
            };

            if (!CancelledOrders.TryAdd(orderCode, cancellationInfo))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng đã được xử lý trước đó."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Đơn hàng đã được hủy thành công.",
                orderCode,
                status = "Cancelled"
            });
        }

        internal static MyOrdersViewModel GetMockMyOrders()
        {
            var order = GetMockOrder();

            var orders = new List<OrderListItemViewModel>
    {
        new()
        {
            OrderCode = order.OrderCode,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            StatusLabel = order.StatusLabel,
            StatusColor = order.StatusColor,
            PaymentMethodLabel = order.PaymentMethodLabel,
            PaymentStatusLabel = order.PaymentStatusLabel,
            PaymentMethod = "COD",
PaymentStatus = "Unpaid",
            TotalItems = order.Items.Sum(x => x.Quantity),
            TotalAmount = order.TotalAmount,
            FirstProductName = order.Items.FirstOrDefault()?.ProductName,
            FirstProductImage = order.Items.FirstOrDefault()?.ImageUrl,
            CanCancel = order.Status == "Pending"
        },
        new()
        {
            OrderCode = "CTN-20261110-0002",
            CreatedAt = new DateTime(2026, 11, 10, 10, 15, 0),
            Status = "Shipping",
            StatusLabel = "Đang giao hàng",
            StatusColor = "info",
            PaymentMethod = "VNPay",
            PaymentMethodLabel = "VNPay",
            PaymentStatus = "Failed",
            PaymentStatusLabel = "Thanh toán thất bại",
            TotalItems = 2,
            TotalAmount = 178000m,
            FirstProductName = "Nước rửa tay Caterin hương hoa sen",
            FirstProductImage = "/images/products/handwash-lotus-500ml.webp"
        },
        new()
        {
            OrderCode = "CTN-20261108-0003",
            CreatedAt = new DateTime(2026, 11, 8, 9, 30, 0),
            Status = "Completed",
            StatusLabel = "Giao hàng thành công",
            StatusColor = "success",
            PaymentMethodLabel = "COD",
            PaymentStatusLabel = "Đã thanh toán",
            PaymentMethod = "COD",
PaymentStatus = "Paid",
            TotalItems = 1,
            TotalAmount = 89000m,
            FirstProductName = "Nước rửa chén Caterin tinh dầu quế cam",
            FirstProductImage = "/images/products/dishwashing-orange-cinnamon-750ml.webp",
            ReviewableItemCount = 1,
            ReviewedItemCount = 0,
            CanReview = true
        },
        new()
        {
            OrderCode = "CTN-20261109-0004",
            CreatedAt = new DateTime(2026, 11, 9, 14, 20, 0),
            Status = "Confirmed",
            StatusLabel = "Đã xác nhận",
            StatusColor = "info",
            PaymentMethodLabel = "COD",
            PaymentMethod = "COD",
PaymentStatus = "Unpaid",
            PaymentStatusLabel = "Chưa thanh toán",
            TotalItems = 2,
            TotalAmount = 158000m,
            FirstProductName = "Nước rửa tay Caterin hương hoa sen",
            FirstProductImage = "/images/products/handwash-lotus-500ml.webp",
            CanCancel = true
        }
    };

            return new MyOrdersViewModel
            {
                TotalOrders = orders.Count,
                Orders = orders
            };
        }

        [Authorize]
        [HttpGet("{orderCode}/return-request")]
        public IActionResult ReturnRequest(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return BadRequest();

            var order = GetMockMyOrders().Orders.FirstOrDefault(x =>
                string.Equals(
                    x.OrderCode,
                    orderCode,
                    StringComparison.OrdinalIgnoreCase));

            if (order == null)
                return NotFound();

            if (CancelledOrders.ContainsKey(orderCode))
                return BadRequest("Đơn hàng đã bị hủy.");

            if (order.Status != "Completed")
                return BadRequest("Chỉ đơn hàng đã giao thành công mới được yêu cầu đổi trả.");

            if (ReturnRequests.ContainsKey(orderCode))
            {
                return BadRequest("Đơn hàng đã có yêu cầu đổi trả.");
            }

            var model = GetMockReturnRequest(orderCode);

            if (model == null)
                return NotFound();

            if (model.ReceivedAt == null ||
                DateTime.Now > model.ReturnDeadline)
            {
                return BadRequest("Đơn hàng đã hết thời hạn yêu cầu đổi trả.");
            }

            return View(model);
        }
        [Authorize]
        [HttpPost("{orderCode}/return-request")]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReturnRequest(
    string orderCode,
    [FromBody] ReturnRequestViewModel model)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Mã đơn hàng không hợp lệ."
                });
            }

            if (model == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu yêu cầu không hợp lệ."
                });
            }

            var order = GetMockMyOrders().Orders.FirstOrDefault(x =>
                string.Equals(
                    x.OrderCode,
                    orderCode,
                    StringComparison.OrdinalIgnoreCase));

            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy đơn hàng."
                });
            }

            if (CancelledOrders.ContainsKey(orderCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng đã bị hủy."
                });
            }

            if (order.Status != "Completed")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Chỉ đơn hàng đã giao thành công mới được yêu cầu đổi trả."
                });
            }

            if (ReturnRequests.ContainsKey(orderCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng đã có yêu cầu đổi trả."
                });
            }

            var mockRequest = GetMockReturnRequest(orderCode);

            if (mockRequest?.ReceivedAt == null ||
                DateTime.Now > mockRequest.ReturnDeadline)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Đơn hàng đã hết thời hạn yêu cầu đổi trả."
                });
            }

            if (model.RequestType is not ("Refund" or "Exchange"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Hình thức xử lý không hợp lệ."
                });
            }

            var allowedReasons = new[]
            {
        "Damaged",
        "WrongProduct",
        "MissingItem",
        "QualityIssue",
        "NotAsDescribed",
        "Other"
    };

            if (!allowedReasons.Contains(model.Reason))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lý do đổi trả không hợp lệ."
                });
            }

            if (model.Reason == "Other" &&
                string.IsNullOrWhiteSpace(model.Description))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng mô tả lý do đổi trả."
                });
            }

            if (model.Description?.Length > 1000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Mô tả không được vượt quá 1000 ký tự."
                });
            }

            var selectedItems = model.Items?
                .Where(x => x.IsSelected)
                .ToList();

            if (selectedItems == null || selectedItems.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng chọn ít nhất một sản phẩm cần đổi trả."
                });
            }

            // Đối chiếu sản phẩm và số lượng với dữ liệu đơn hàng gốc.
            foreach (var selected in selectedItems)
            {
                var purchasedItem = mockRequest.Items.FirstOrDefault(x =>
                    string.Equals(
                        x.ProductName,
                        selected.ProductName,
                        StringComparison.OrdinalIgnoreCase));

                if (purchasedItem == null ||
                    selected.ReturnQuantity < 1 ||
                    selected.ReturnQuantity > purchasedItem.PurchasedQuantity)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Sản phẩm hoặc số lượng đổi trả không hợp lệ."
                    });
                }
            }

            var requestInfo = new MockReturnRequestInfo
            {
                RequestCode = $"RTN-{DateTime.Now:yyyyMMddHHmmssfff}",
                OrderCode = orderCode,
                RequestType = model.RequestType,
                Reason = model.Reason,
                Description = model.Description?.Trim(),
                RequestedAt = DateTime.Now,

                Status = "Pending",
                StatusLabel = "Chờ xác nhận",
                StatusColor = "warning",

                Items = selectedItems.Select(selected =>
                {
                    var purchasedItem = mockRequest.Items.First(x =>
                        string.Equals(
                            x.ProductName,
                            selected.ProductName,
                            StringComparison.OrdinalIgnoreCase));

                    return new MockReturnRequestItem
                    {
                        ProductName = purchasedItem.ProductName,
                        VariantName = purchasedItem.VariantName,
                        ImageUrl = purchasedItem.ImageUrl,
                        UnitPrice = purchasedItem.UnitPrice,
                        PurchasedQuantity = purchasedItem.PurchasedQuantity,
                        ReturnQuantity = selected.ReturnQuantity
                    };
                }).ToList()
            };

            if (!ReturnRequests.TryAdd(orderCode, requestInfo))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Yêu cầu đổi trả đã được gửi trước đó."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Yêu cầu đổi trả đã được gửi thành công.",
                orderCode,
                requestCode = requestInfo.RequestCode,
                status = requestInfo.Status,
                redirectUrl = Url.Action(
        nameof(ReturnRequestDetails),
        "Order",
        new { requestCode = requestInfo.RequestCode })
            });
        }
        [Authorize]
        [HttpGet("return-request/{requestCode}")]
        public IActionResult ReturnRequestDetails(string requestCode)
        {
            if (string.IsNullOrWhiteSpace(requestCode))
                return NotFound();

            var request = ReturnRequests.Values.FirstOrDefault(x =>
                string.Equals(
                    x.RequestCode,
                    requestCode,
                    StringComparison.OrdinalIgnoreCase));

            if (request == null)
                return NotFound();

            var model = new ReturnRequestDetailsViewModel
            {
                RequestCode = request.RequestCode,
                OrderCode = request.OrderCode,

                RequestType = request.RequestType,
                RequestTypeLabel = request.RequestType switch
                {
                    "Refund" => "Hoàn tiền",
                    "Exchange" => "Đổi sản phẩm",
                    _ => request.RequestType
                },

                Reason = request.Reason,
                ReasonLabel = GetReturnReasonLabel(request.Reason),
                Description = request.Description,

                Status = request.Status,
                StatusLabel = request.StatusLabel,
                StatusColor = request.StatusColor,

                RequestedAt = request.RequestedAt,

                Items = request.Items.Select(x =>
                    new ReturnRequestDetailsItemViewModel
                    {
                        ProductName = x.ProductName,
                        VariantName = x.VariantName,
                        ImageUrl = x.ImageUrl,
                        UnitPrice = x.UnitPrice,
                        PurchasedQuantity = x.PurchasedQuantity,
                        ReturnQuantity = x.ReturnQuantity
                    }).ToList()
            };

            return View(model);
        }


        [Authorize]
        [HttpGet("return-requests")]
        public IActionResult ReturnRequestHistory(string? status = null)
        {
            var requests = ReturnRequests.Values
                .AsEnumerable()
                .OrderByDescending(x => x.RequestedAt)
                .ToList();

            if (!string.IsNullOrWhiteSpace(status))
            {
                requests = requests
                    .Where(x => string.Equals(
                        x.Status,
                        status,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var allRequests = ReturnRequests.Values.ToList();

            var model = new ReturnRequestHistoryViewModel
            {
                StatusFilter = status,

                TotalRequests = allRequests.Count,

                PendingRequests = allRequests.Count(x =>
                    x.Status == "Pending"),

                ProcessingRequests = allRequests.Count(x =>
                    x.Status == "Processing"),

                CompletedRequests = allRequests.Count(x =>
                    x.Status == "Completed" || x.Status == "Approved"),

                Requests = requests.Select(x => new ReturnRequestHistoryItemViewModel
                {
                    RequestCode = x.RequestCode,
                    OrderCode = x.OrderCode,

                    RequestType = x.RequestType,
                    RequestTypeLabel = x.RequestType switch
                    {
                        "Refund" => "Hoàn tiền",
                        "Exchange" => "Đổi sản phẩm",
                        _ => x.RequestType
                    },

                    Reason = x.Reason,
                    ReasonLabel = GetReturnReasonLabel(x.Reason),

                    Status = x.Status,
                    StatusLabel = x.StatusLabel,
                    StatusColor = x.StatusColor,

                    RequestedAt = x.RequestedAt,

                    TotalItems = x.Items.Count,
                    TotalReturnAmount = x.Items.Sum(i =>
                        i.UnitPrice * i.ReturnQuantity),

                    FirstProductName = x.Items.FirstOrDefault()?.ProductName,
                    FirstProductImage = x.Items.FirstOrDefault()?.ImageUrl
                }).ToList()
            };

            return View(model);
        }

        [Authorize]
        [HttpGet("{orderCode}/review")]
        public IActionResult Review(string orderCode)
        {
            var model = BuildProductReviewViewModel(orderCode);

            if (model == null)
                return NotFound();

            if (model.Items.All(x => x.IsReviewed))
            {
                TempData["InfoMessage"] = "Bạn đã đánh giá tất cả sản phẩm trong đơn hàng này.";

                return RedirectToAction(
                    nameof(Details),
                    new { orderCode });
            }

            return View(model);
        }



        [Authorize]
        [HttpPost("{orderCode}/review")]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(
            string orderCode,
            ProductReviewViewModel model)
        {
            var reviewModel = BuildProductReviewViewModel(orderCode);

            if (reviewModel == null)
                return NotFound();

            // Kiểm tra đơn hàng đã được đánh giá hết chưa.
            if (reviewModel.Items.All(x => x.IsReviewed))
            {
                return Conflict(new
                {
                    success = false,
                    message = "Bạn đã đánh giá tất cả sản phẩm trong đơn hàng này."
                });
            }

            var submittedItems = model.Items ?? [];

            var validItems = reviewModel.Items
                .Where(x => !x.IsReviewed)
                .ToDictionary(x => x.ItemIndex);

            var submittedIndexes = new HashSet<int>();
            var reviewsToSave = new List<MockProductReviewInfo>();

            if (!submittedItems.Any())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Vui lòng đánh giá ít nhất một sản phẩm.");
            }

            foreach (var submitted in submittedItems)
            {
                if (!validItems.TryGetValue(submitted.ItemIndex, out var original))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Sản phẩm đánh giá không hợp lệ.");
                    continue;
                }

                if (!submittedIndexes.Add(submitted.ItemIndex))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Dữ liệu đánh giá bị trùng lặp.");
                    continue;
                }

                if (submitted.Rating < 1 || submitted.Rating > 5)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Vui lòng chọn số sao cho sản phẩm {original.ProductName}.");
                    continue;
                }

                var comment = submitted.Comment?.Trim();

                if (comment?.Length > 1000)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Nội dung đánh giá không được vượt quá 1.000 ký tự.");
                    continue;
                }

                reviewsToSave.Add(new MockProductReviewInfo
                {
                    OrderCode = orderCode,
                    ItemIndex = original.ItemIndex,
                    ProductName = original.ProductName,
                    Rating = submitted.Rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                });
            }

            // Trả JSON lỗi để JavaScript xử lý bằng SweetAlert2.
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState
                    .Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                return BadRequest(new
                {
                    success = false,
                    message = errorMessage ?? "Thông tin đánh giá không hợp lệ."
                });
            }

            // Kiểm tra và lưu đồng bộ để tránh lưu một phần.
            lock (ProductReviewLock)
            {
                var keys = reviewsToSave
                    .Select(x => GetProductReviewKey(x.OrderCode, x.ItemIndex))
                    .ToList();

                if (keys.Any(key => ProductReviews.ContainsKey(key)))
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Một sản phẩm đã được đánh giá trước đó. Vui lòng tải lại trang."
                    });
                }

                foreach (var review in reviewsToSave)
                {
                    var key = GetProductReviewKey(
                        review.OrderCode,
                        review.ItemIndex);

                    ProductReviews.TryAdd(key, review);
                }
            }

            TempData["SuccessMessage"] =
                "Cảm ơn bạn đã đánh giá sản phẩm tại Caterin!";

            return Json(new
            {
                success = true,
                message = "Đánh giá sản phẩm thành công.",
                redirectUrl = Url.Action(
                    nameof(Details),
                    "Order",
                    new { orderCode })
            });
        }




        private static string GetReturnReasonLabel(string reason)
        {
            return reason switch
            {
                "Damaged" => "Sản phẩm bị hư hỏng",
                "WrongProduct" => "Giao sai sản phẩm",
                "MissingItem" => "Thiếu sản phẩm",
                "QualityIssue" => "Vấn đề về chất lượng",
                "NotAsDescribed" => "Không đúng mô tả",
                "Other" => "Lý do khác",
                _ => reason
            };
        }

        private static OrderDetailsViewModel GetMockOrder()
        {
            var createdAt = new DateTime(2026, 11, 11, 14, 30, 0);

            return new OrderDetailsViewModel
            {
                OrderCode = "CTN-20261111-0001",
                CreatedAt = createdAt,

                // Order status
                Status = "Pending",
                StatusLabel = "Chờ xác nhận",
                StatusColor = "warning",

                // Payment
                PaymentMethod = "COD",
                PaymentMethodLabel = "Thanh toán khi nhận hàng",
                PaymentStatus = "Pending",
                PaymentStatusLabel = "Chưa thanh toán",

                // Shipping
                CarrierName = null,
                TrackingNumber = null,
                EstimatedDeliveryAt = createdAt.AddDays(3),

                // Customer
                RecipientName = "Nguyễn Văn An",
                PhoneNumber = "0931 234 567",
                Email = "an.nguyen@example.com",
                ShippingAddress =
                    "123 Nguyễn Văn Cừ, Phường Xuân An, Long Khánh, Đồng Nai",
                ShippingNote = "Gọi điện trước khi giao hàng.",

                // Amounts
                SubTotal = 248000m,
                ShippingFee = 30000m,
                DiscountAmount = 20000m,
                TotalAmount = 258000m,

                // Actions
                CanCancel = true,
                CanReview = false,
                CanRequestReturn = false,

                CustomerNote = "Vui lòng giao hàng trong giờ hành chính.",

                Items =
                [
                    new OrderItemViewModel
            {
                ProductName = "Nước rửa chén Caterin tinh dầu quế cam",
                VariantName = "Chai 750ml",
                ImageUrl = "/images/products/dishwashing-orange-cinnamon-750ml.webp",
                UnitPrice = 89000m,
                Quantity = 2
            },
            new OrderItemViewModel
            {
                ProductName = "Nước rửa tay Caterin hương hoa sen",
                VariantName = "Chai 500ml",
                ImageUrl = "/images/products/handwash-lotus-500ml.webp",
                UnitPrice = 70000m,
                Quantity = 1
            }
                ],

                Timeline =
                [
                    new OrderTimelineItemViewModel
            {
                Title = "Đặt hàng thành công",
                Description = "Đơn hàng đã được ghi nhận trên hệ thống.",
                OccurredAt = createdAt,
                IsCompleted = true,
                IsCurrent = false,
                Icon = "ph-check-circle"
            },
            new OrderTimelineItemViewModel
            {
                Title = "Chờ xác nhận",
                Description = "Caterin đang tiếp nhận và kiểm tra đơn hàng.",
                OccurredAt = createdAt,
                IsCompleted = true,
                IsCurrent = true,
                Icon = "ph-clock"
            },
            new OrderTimelineItemViewModel
            {
                Title = "Đang chuẩn bị hàng",
                Description = "Đơn hàng sẽ được đóng gói sau khi xác nhận.",
                OccurredAt = null,
                IsCompleted = false,
                IsCurrent = false,
                Icon = "ph-package"
            },
            new OrderTimelineItemViewModel
            {
                Title = "Đang giao hàng",
                Description = "Đơn hàng đang trên đường đến bạn.",
                OccurredAt = null,
                IsCompleted = false,
                IsCurrent = false,
                Icon = "ph-truck"
            },
            new OrderTimelineItemViewModel
            {
                Title = "Giao hàng thành công",
                Description = "Đơn hàng đã được giao đến người nhận.",
                OccurredAt = null,
                IsCompleted = false,
                IsCurrent = false,
                Icon = "ph-house-line"
            }
                ]
            };
        }


        private static OrderTrackingViewModel GetMockTracking()
        {
            return new OrderTrackingViewModel
            {
                OrderCode = "CTN-20261111-0001",
                Contact = "0931 234 567",

                Order = GetMockOrder(),

                ErrorMessage = null
            };
        }


        private static OrderTrackingViewModel GetMockTrackingError()
        {
            return new OrderTrackingViewModel
            {
                OrderCode = "CTN-20261111-9999",
                Contact = "0931 234 567",
                Order = null,
                ErrorMessage = "Không tìm thấy đơn hàng phù hợp. Vui lòng kiểm tra lại mã đơn hàng và thông tin liên hệ."
            };
        }
        
        private static readonly ConcurrentDictionary<string, MockCancellationInfo>
    CancelledOrders = new(StringComparer.OrdinalIgnoreCase);
        internal static readonly ConcurrentDictionary<string, string>
    MockPaymentStates = new(StringComparer.OrdinalIgnoreCase);
        private sealed class MockCancellationInfo
        {
            public string Reason { get; init; } = string.Empty;
            public DateTime CancelledAt { get; init; }
        }

        private static readonly ConcurrentDictionary<string, MockReturnRequestInfo>
    ReturnRequests = new(StringComparer.OrdinalIgnoreCase);

        private sealed class MockReturnRequestInfo
        {
            public string RequestCode { get; init; } = string.Empty;
            public string OrderCode { get; init; } = string.Empty;

            public string RequestType { get; init; } = string.Empty;
            public string Reason { get; init; } = string.Empty;
            public string? Description { get; init; }

            public DateTime RequestedAt { get; init; }

            public string Status { get; set; } = "Pending";
            public string StatusLabel { get; set; } = "Chờ xác nhận";
            public string StatusColor { get; set; } = "warning";

            public List<MockReturnRequestItem> Items { get; init; } = [];
        }

        private sealed class MockReturnRequestItem
        {
            public string ProductName { get; init; } = string.Empty;
            public string? VariantName { get; init; }
            public string? ImageUrl { get; init; }
            public decimal UnitPrice { get; init; }
            public int PurchasedQuantity { get; init; }
            public int ReturnQuantity { get; init; }
        }
        
        private static ReturnRequestViewModel? GetMockReturnRequest(string orderCode)
        {
            if (!string.Equals(
                orderCode,
                "CTN-20261108-0003",
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var receivedAt = new DateTime(2026, 11, 10, 14, 0, 0);

            return new ReturnRequestViewModel
            {
                OrderCode = orderCode,
                ReceivedAt = receivedAt,
                ReturnDeadline = receivedAt.AddDays(7),

                RecipientName = "Nguyễn Văn An",
                PhoneNumber = "0931 234 567",

                RequestType = "Refund",

                Items =
                [
                    new ReturnRequestItemViewModel
            {
                ProductName = "Nước rửa chén Caterin tinh dầu quế cam",
                VariantName = "Chai 750ml",
                ImageUrl = "/images/products/dishwashing-orange-cinnamon-750ml.webp",
                UnitPrice = 89000m,
                PurchasedQuantity = 1,
                IsSelected = true,
                ReturnQuantity = 1
            }
                ]
            };
        }



        private static readonly ConcurrentDictionary<string, MockProductReviewInfo>
    ProductReviews = new(StringComparer.OrdinalIgnoreCase);
        private static string GetProductReviewKey(string orderCode, int itemIndex)
        {
            return $"{orderCode}:{itemIndex}";
        }
        private class MockProductReviewInfo
        {
            public string OrderCode { get; set; } = string.Empty;

            public int ItemIndex { get; set; }

            public string ProductName { get; set; } = string.Empty;

            public int Rating { get; set; }

            public string? Comment { get; set; }

            public DateTime CreatedAt { get; set; }
        }


        private ProductReviewViewModel? BuildProductReviewViewModel(string orderCode)
        {
            var order = GetMockMyOrders().Orders.FirstOrDefault(x =>
                x.OrderCode.Equals(
                    orderCode,
                    StringComparison.OrdinalIgnoreCase));

            if (order == null ||
                order.Status != "Completed" ||
                CancelledOrders.ContainsKey(order.OrderCode))
            {
                return null;
            }

            var model = new ProductReviewViewModel
            {
                OrderCode = order.OrderCode,
                OrderCreatedAt = order.CreatedAt,
                Items =
                [
                    new ProductReviewItemViewModel
            {
                ItemIndex = 0,
                ProductName = order.FirstProductName,
                ImageUrl = order.FirstProductImage,
                PurchasedQuantity = 1
            }
                ]
            };

            foreach (var item in model.Items)
            {
                var key = GetProductReviewKey(
                    order.OrderCode,
                    item.ItemIndex);

                if (ProductReviews.TryGetValue(key, out var review))
                {
                    item.IsReviewed = true;
                    item.Rating = review.Rating;
                    item.Comment = review.Comment;
                }
            }

            return model;
        }

        private static readonly object ProductReviewLock = new();
    }
}
