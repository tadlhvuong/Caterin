using Shared.DTOs.Identity;
using Shared.DTOs.Order;
using Shared.Enums;
using Shared.Requests.Order;
using Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Core
{
    public interface IOrderService
    {
        Task<ServiceResult<PagedResult<OrderListItemResult>>> GetOrdersAsync(OrderListRequest request, CancellationToken cancellationToken = default);
        
        Task<OrderDetailsDto?> GetDetailsAsync(int orderId, CancellationToken cancellationToken = default);
        
        Task<ServiceResult<int>> CreateAsync(CreateOrderRequest request);

        Task<ServiceResult> UpdateAsync(int id, UpdateOrderRequest request);

        Task<ServiceResult> ChangeStatusAsync(int id, OrderStatus status, string? note = null);

        Task<ServiceResult> CancelAsync(int id, string? reason = null);

        Task<ServiceResult> DeleteAsync(int id);
    }
}
