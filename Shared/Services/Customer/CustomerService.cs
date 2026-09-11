using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Data.Context;
using Shared.DTOs.Customer;
using Shared.DTOs.Identity;
using Shared.DTOs.Product;
using Shared.Enums;
using Shared.Interfaces.Core;
using Shared.Interfaces.Media;
using Shared.Responses.Datatables;

namespace Shared.Services.Customer
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _dbContext;

        private readonly ILogger<CustomerService> _logger;
        public CustomerService(AppDbContext dbContext, ILogger<CustomerService> logger)
        {
            _dbContext = dbContext;

            _logger = logger;
        }
        public async Task<PagedResult<CustomerListResult>> GetCustomersAsync(CustomerDataTableRequest request)
        {
            var query = _dbContext.Users.AsNoTracking().AsQueryable();
            var totalCount = await query.CountAsync();
            // Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x => x.UserName.Contains(search) || (x.PhoneNumber != null && x.PhoneNumber.Contains(search)) ||
                    (x.Email != null && x.Email.Contains(search)));
            }

            // Status
            if (request.Status.HasValue)
            {
                var isActive = request.Status.Value == 1;

                query = query.Where(x => x.IsActive == isActive);
            }

            // Total records sau filter
            var totalRecords = await query.CountAsync();

            // Sort
            query = request.SortColumn?.ToLower() switch
            {
                "name" => request.SortDirection == "asc"
                    ? query.OrderBy(x => x.UserName)
                    : query.OrderByDescending(x => x.UserName),

                "phone" => request.SortDirection == "asc"
                    ? query.OrderBy(x => x.PhoneNumber)
                    : query.OrderByDescending(x => x.PhoneNumber),

                "email" => request.SortDirection == "asc"
                    ? query.OrderBy(x => x.Email)
                    : query.OrderByDescending(x => x.Email),

                //"ordercount" => request.SortDirection == "asc"
                //    ? query.OrderBy(x => x.Orders.Count())
                //    : query.OrderByDescending(x => x.Orders.Count()),

                //"totalspent" => request.SortDirection == "asc"
                //    ? query.OrderBy(x => x.Orders
                //        .Where(o => o.Status == OrderStatus.Completed)
                //        .Sum(o => o.Total))
                //    : query.OrderByDescending(x => x.Orders
                //        .Where(o => o.Status == OrderStatus.Completed)
                //        .Sum(o => o.Total)),

                //"lastorderat" => request.SortDirection == "asc"
                //    ? query.OrderBy(x => x.Orders
                //        .Max(o => (DateTime?)o.CreatedAt))
                //    : query.OrderByDescending(x => x.Orders
                //        .Max(o => (DateTime?)o.CreatedAt)),

                "createdat" => request.SortDirection == "asc"
                    ? query.OrderBy(x => x.CreatedAt)
                    : query.OrderByDescending(x => x.CreatedAt),

                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var filteredCount = query.Count();

            // Paging + Projection
            var data = await query
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new CustomerListResult
                {
                    Id = x.Id,
                    Name = x.UserName,
                    Phone = x.PhoneNumber,
                    Email = x.Email,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,

                    //OrderCount = x.Orders.Count(),

                    //TotalSpent = x.Orders
                    //    .Where(o => o.Status == OrderStatus.Completed)
                    //    .Sum(o => o.Total),

                    //LastOrderAt = x.Orders
                    //    .OrderByDescending(o => o.CreatedAt)
                    //    .Select(o => (DateTime?)o.CreatedAt)
                    //    .FirstOrDefault()

                    //Demo
                    OrderCount = 1000,
                    TotalSpent = 1234567000,
                    LastOrderAt = DateTime.UtcNow,
                }).ToListAsync();

            return new PagedResult<CustomerListResult>
            {
                Items = data,

                Page = request.Length > 0 ? request.Start / request.Length + 1 : 1,

                PageSize = request.Length,

                TotalCount = totalCount,

                FilteredCount = filteredCount
            };
        }
    }
}
