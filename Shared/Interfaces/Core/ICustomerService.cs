using Shared.DTOs.Customer;
using Shared.DTOs.Identity;
using Shared.Responses.Datatables;

namespace Shared.Interfaces.Core
{
    public interface ICustomerService
    {
        /// <summary>
        /// Show customer list
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PagedResult<CustomerListResult>> GetCustomersAsync(CustomerDataTableRequest request);
    }
}
