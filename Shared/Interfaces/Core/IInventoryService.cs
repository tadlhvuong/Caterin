using Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Core
{
    public interface IInventoryService
    {
        Task<int> GetAvailableStockAsync(int productId);

        Task<bool> HasEnoughStockAsync(int productId, int quantity);

        Task<ServiceResult> ValidateStockAsync(IReadOnlyDictionary<int, int> items);

        Task<ServiceResult> ReserveAsync(int orderId, IReadOnlyDictionary<int, int> items);

        Task<ServiceResult> ReleaseAsync( int orderId);

        Task<ServiceResult> ConfirmAsync(int orderId);

        Task<ServiceResult> IncreaseAsync(int productId, int quantity, string? note = null);

        Task<ServiceResult> DecreaseAsync(int productId, int quantity, string? note = null);

        Task<ServiceResult> AdjustAsync(int productId, int quantity, string? note = null);
    }
}
