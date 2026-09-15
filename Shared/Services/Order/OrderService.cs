using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Data.Context;
using Shared.Data.Entities.Order;
using Shared.DTOs.Identity;
using Shared.DTOs.Order;
using Shared.DTOs.Product;
using Shared.Enums;
using Shared.Interfaces.Core;
using Shared.Requests.Order;
using Shared.Responses;
using Shared.Responses.Datatables;

namespace Shared.Services.Order
{
    public class OrderService : IOrderService
    {

        private readonly AppDbContext _dbContext;
        private readonly ILogger<OrderService> _logger;
        public OrderService(AppDbContext dbContext, ILogger<OrderService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        public async Task<PagedResult<OrderListItemResult>> GetOrdersAsync(OrderDataTableRequest request)
        {
            var query = _dbContext.Orders.AsNoTracking().AsQueryable();

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x => x.OrderCode.Contains(search));
            }

            if (request.OrderStatus.HasValue && Enum.IsDefined(typeof(ProductStatus), request.OrderStatus.Value))
            {
                var status = (OrderStatus)request.OrderStatus.Value;

                query = query.Where(x => x.Status == status);
            }

            var filteredCount = await query.CountAsync();
            var sortColumn = request.SortColumn?.ToLower();

            query = request.SortColumn switch
            {
                "OrderCode" => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.OrderCode)
                    : query.OrderBy(x => x.OrderCode),

                "Status" => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.Status)
                    : query.OrderBy(x => x.Status),

                "TotalAmount" => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.TotalAmount)
                    : query.OrderBy(x => x.TotalAmount),

                "ItemCount" => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.Items.Count)
                    : query.OrderBy(x => x.Items.Count),

                _ => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt)
            };
            var items = await query.Skip(request.Start).Take(request.Length).Select(x => new OrderListItemResult
     {
         Id = x.Id,
         Order = x.OrderCode,

         Date = x.CreatedAt,

         Time = x.CreatedAt.ToString("HH:mm"),

         Customer = x.User.UserName,
         Email = x.User.Email,

         Avatar = x.User.Avatar,
         PaymentStatus = PaymentStatus.Processing,

         Method = "Chưa rõ",

         MethodNumber = "Chưa rõ",
         //PaymentStatus = x.PaymentStatus,

         //Method = x.PaymentMethod,

         //MethodNumber = x.PaymentMethodNumber,

         Status = x.Status,

         TotalAmount = x.TotalAmount,

         ItemCount = x.Items.Count
     })
     .ToListAsync();

            return new PagedResult<OrderListItemResult>
            {
                Items = items,

                Page = request.Length > 0 ? request.Start / request.Length + 1 : 1,

                PageSize = request.Length,

                TotalCount = totalCount,

                FilteredCount = filteredCount
            };

        }

        public async Task<OrderDetailsDto?> GetDetailsAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _dbContext.Orders
                .AsNoTracking()
                .Where(x => x.Id == orderId)
                .Select(x => new OrderDetailsDto
                {
                    Id = x.Id,
                    OrderCode = x.OrderCode,

                    Status = x.Status,
                    //PaymentStatus = x.PaymentStatus,

                    SubTotal = x.SubTotal,
                    //ShippingFee = x.ShippingFee,
                    DiscountAmount = x.DiscountAmount,
                    TotalAmount = x.TotalAmount,

                    CreatedAt = x.CreatedAt,

                    ShippingAddress = x.Address == null
                        ? null
                        : new OrderAddressDto
                        {
                            FullName = x.Address.ReceiverName,
                            Phone = x.Address.Phone,
                            Address = x.Address.AddressLine,
                            Ward = x.Address.Ward,
                            District = x.Address.District,
                            Province = x.Address.Province
                        },

                    Items = x.Items
                        .Select(i => new OrderItemDto
                        {
                            Id = i.Id,
                            ProductId = i.Id,
                            ProductName = i.ProductName,
                            ProductImage = null,
                            UnitPrice = i.Price,
                            Quantity = i.Quantity,
                            TotalPrice = i.Total
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return order;
        }

        public async Task<ServiceResult<int>> CreateAsync(
    CreateOrderRequest request)
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (request.Items == null || request.Items.Count == 0)
                {
                    return ServiceResult<int>.Fail(
                        "Order must contain at least one item.");
                }

                if (request.Items.Any(x => x.Quantity <= 0))
                {
                    return ServiceResult<int>.Fail(
                        "Product quantity must be greater than zero.");
                }

                // ==========================================
                // Get Product Variants
                // ==========================================

                var variantIds = request.Items
                    .Select(x => x.ProductVariantId)
                    .Distinct()
                    .ToList();

                var variants = await _dbContext.ProductVariants
                    .Include(x => x.Product)
                    .Where(x => variantIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);

                if (variants.Count != variantIds.Count)
                {
                    return ServiceResult<int>.Fail(
                        "One or more product variants do not exist.");
                }

                // ==========================================
                // Create Order
                // ==========================================

                var order = new Data.Entities.Order.Order
                {
                    OrderCode = await GenerateOrderCodeAsync(),
                    UserId = request.UserId,

                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.Pending,

                    CreatedAt = DateTime.UtcNow,

                    SubTotal = 0,
                    ShippingAmount = 0,
                    DiscountAmount = 0,
                    TotalAmount = 0
                };

                decimal subTotal = 0;

                // ==========================================
                // Reserve Stock + Create OrderItems
                // ==========================================
                var items = request.Items.GroupBy(x => x.ProductVariantId)
                            .Select(g => new
                            {
                                ProductVariantId = g.Key,
                                Quantity = g.Sum(x => x.Quantity)
                            }).ToList();
                foreach (var item in request.Items)
                {
                    var variant = variants[item.ProductVariantId];

                    if (!variant.IsActive)
                    {
                        return ServiceResult<int>.Fail(
                            $"Product variant '{variant.Name}' is not available.");
                    }

                    // Available stock
                    var affected = await _dbContext.InventoryStocks
     .Where(x =>
         x.WarehouseId == item.WarehouseId &&
         x.ProductVariantId == item.ProductVariantId &&
         x.AvailableQuantity >= item.Quantity)
     .ExecuteUpdateAsync(setters => setters
         .SetProperty(
             x => x.AvailableQuantity,
             x => x.AvailableQuantity - item.Quantity)
         .SetProperty(
             x => x.ReservedQuantity,
             x => x.ReservedQuantity + item.Quantity));

                    if (affected == 0)
                    {
                        return ServiceResult<int>.Fail(
                            $"Product variant '{variant.Name}' does not have enough stock.");
                    }

                    // ======================================
                    // Calculate item total
                    // ======================================

                    var totalPrice =
                        variant.Price * item.Quantity;

                    // ======================================
                    // OrderItem snapshot
                    // ======================================

                    var orderItem = new OrderItem
                    {
                        Order = order,

                        ProductVariantId = variant.Id,

                        ProductName = variant.Product.Name,
                        VariantName = variant.Name,

                        Price = variant.Price,
                        Quantity = item.Quantity,
                        Total = totalPrice,

                        CreatedAt = DateTime.UtcNow
                    };

                    order.Items.Add(orderItem);

                    subTotal += totalPrice;
                }

                // ==========================================
                // Order totals
                // ==========================================

                order.SubTotal = subTotal;

                order.ShippingAmount =
                    await CalculateShippingFeeAsync(request);

                order.DiscountAmount =
                    await CalculateDiscountAsync(
                        request,
                        subTotal);

                order.TotalAmount =
                    order.SubTotal
                    + order.ShippingAmount
                    - order.DiscountAmount;

                // ==========================================
                // Shipping Address Snapshot
                // ==========================================

                order.Address = new OrderAddress
                {
                    ReceiverName = request.ShippingAddress.FullName,
                    Phone = request.ShippingAddress.Phone,
                    AddressLine = request.ShippingAddress.Address,
                    Ward = request.ShippingAddress.Ward,
                    District = request.ShippingAddress.District,
                    Province = request.ShippingAddress.Province
                };

                // ==========================================
                // Initial History
                // ==========================================

                order.Histories.Add(new OrderHistory
                {
                    Status = OrderStatus.Pending,
                    Note = "Order created and stock reserved.",
                    CreatedAt = DateTime.UtcNow
                });

                // ==========================================
                // Save
                // ==========================================

                _dbContext.Orders.Add(order);

                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return ServiceResult<int>.Success(order.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<ServiceResult> UpdateAsync(int id, UpdateOrderRequest request)
        {
            var order = await _dbContext.Orders
                .Include(x => x.Address)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                return ServiceResult.Fail(
                    "Order not found.");
            }

            // Không cho sửa Order đã hoàn thành / hủy / hoàn tiền
            if (order.Status is OrderStatus.Completed
                or OrderStatus.Cancelled
                or OrderStatus.Refunded)
            {
                return ServiceResult.Fail(
                    "This order cannot be updated.");
            }

            if (request.ShippingFee < 0)
            {
                return ServiceResult.Fail(
                    "Shipping fee cannot be negative.");
            }

            if (request.DiscountAmount < 0)
            {
                return ServiceResult.Fail(
                    "Discount amount cannot be negative.");
            }

            if (request.DiscountAmount > order.SubTotal + request.ShippingFee)
            {
                return ServiceResult.Fail(
                    "Discount amount is invalid.");
            }

            // Update shipping fee
            order.ShippingAmount = request.ShippingFee;

            // Update discount
            order.DiscountAmount = request.DiscountAmount;

            // Recalculate total
            order.TotalAmount =
                order.SubTotal
                + order.ShippingAmount
                - order.DiscountAmount;

            // Update shipping address
            if (request.ShippingAddress != null)
            {
                if (order.Address == null)
                {
                    order.Address = new OrderAddress();
                }

                order.Address.ReceiverName =
                    request.ShippingAddress.FullName;

                order.Address.Phone =
                    request.ShippingAddress.Phone;

                order.Address.AddressLine =
                    request.ShippingAddress.Address;

                order.Address.Ward =
                    request.ShippingAddress.Ward;

                order.Address.District =
                    request.ShippingAddress.District;

                order.Address.Province =
                    request.ShippingAddress.Province;
            }

            await _dbContext.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ChangeStatusAsync(
    int id,
    OrderStatus status,
    string? note = null)
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (order == null)
                {
                    return ServiceResult.Fail(
                        "Order not found.");
                }

                var currentStatus = order.Status;

                // Không cho chuyển sang cùng trạng thái
                if (currentStatus == status)
                {
                    return ServiceResult.Fail(
                        "Order is already in this status.");
                }

                // Validate transition
                if (!IsValidStatusTransition(currentStatus, status))
                {
                    return ServiceResult.Fail(
                        $"Cannot change order status from {currentStatus} to {status}.");
                }

                // Update status
                order.Status = status;

                // Create history
                var history = new OrderHistory
                {
                    OrderId = order.Id,
                    Status = status,
                    Note = note,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.OrderHistories.Add(history);

                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return ServiceResult.Success();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ServiceResult> CancelAsync(
    int id,
    string? reason = null)
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (order == null)
                {
                    return ServiceResult.Fail(
                        "Order not found.");
                }

                // Chỉ cho phép hủy ở các trạng thái phù hợp
                if (order.Status is not (
                    OrderStatus.Pending or
                    OrderStatus.Confirmed or
                    OrderStatus.Processing))
                {
                    return ServiceResult.Fail(
                        $"Order cannot be cancelled when status is {order.Status}.");
                }

                // Không bắt buộc reason, nhưng nếu có thì giới hạn độ dài
                if (!string.IsNullOrWhiteSpace(reason) &&
                    reason.Length > 500)
                {
                    return ServiceResult.Fail(
                        "Cancellation reason cannot exceed 500 characters.");
                }

                // Update order status
                order.Status = OrderStatus.Cancelled;

                // Create history
                var history = new OrderHistory
                {
                    OrderId = order.Id,
                    Status = OrderStatus.Cancelled,
                    Note = string.IsNullOrWhiteSpace(reason)
                        ? "Order cancelled."
                        : reason.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.OrderHistories.Add(history);

                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return ServiceResult.Success();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<ServiceResult> DeleteAsync(int id)
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (order == null)
                {
                    return ServiceResult.Fail(
                        "Order not found.");
                }

                // Không cho xóa các đơn đã xử lý
                if (order.Status is not (
                    OrderStatus.Pending or
                    OrderStatus.Cancelled))
                {
                    return ServiceResult.Fail(
                        $"Order cannot be deleted when status is {order.Status}.");
                }

                // Xóa các history
                await _dbContext.OrderHistories
                    .Where(x => x.OrderId == order.Id)
                    .ExecuteDeleteAsync();

                // Xóa address
                await _dbContext.OrderAddresses
                    .Where(x => x.OrderId == order.Id)
                    .ExecuteDeleteAsync();

                // Xóa items
                await _dbContext.OrderItems
                    .Where(x => x.OrderId == order.Id)
                    .ExecuteDeleteAsync();

                // Xóa order
                _dbContext.Orders.Remove(order);

                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return ServiceResult.Success();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<string> GenerateOrderCodeAsync()
        {
            var prefix = $"ORD{DateTime.UtcNow:yyyyMM}";

            var lastCode = await _dbContext.Orders
                .Where(x => x.OrderCode.StartsWith(prefix))
                .OrderByDescending(x => x.Id)
                .Select(x => x.OrderCode)
                .FirstOrDefaultAsync();

            var sequence = 1;

            if (!string.IsNullOrEmpty(lastCode))
            {
                var numberPart = lastCode.Substring(prefix.Length);

                if (int.TryParse(numberPart, out var number))
                {
                    sequence = number + 1;
                }
            }

            return $"{prefix}{sequence:D4}";
        }
        private Task<decimal> CalculateShippingFeeAsync(
    CreateOrderRequest request)
        {
            return Task.FromResult(0m);
        }
        private Task<decimal> CalculateDiscountAsync(
    CreateOrderRequest request,
    decimal subTotal)
        {
            return Task.FromResult(0m);
        }
        private bool IsValidStatusTransition(
    OrderStatus current,
    OrderStatus next)
        {
            return current switch
            {
                OrderStatus.Pending =>
                    next is
                        OrderStatus.Confirmed or
                        OrderStatus.Cancelled,

                OrderStatus.Confirmed =>
                    next is
                        OrderStatus.Processing or
                        OrderStatus.Cancelled,

                OrderStatus.Processing =>
                    next is
                        OrderStatus.Shipping or
                        OrderStatus.Cancelled,

                OrderStatus.Shipping =>
                    next is
                        OrderStatus.Completed,

                OrderStatus.Completed =>
                    next is
                        OrderStatus.Refunded,

                OrderStatus.Cancelled =>
                    false,

                OrderStatus.Refunded =>
                    false,

                _ => false
            };
        }
    }
}
