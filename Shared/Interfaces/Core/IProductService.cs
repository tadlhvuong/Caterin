using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.Data.Entities.Product;
using Shared.DTOs.Identity;
using Shared.DTOs.Product;
using Shared.Enums;
using Shared.Requests;
using Shared.Requests.Product;
using Shared.Requests.Product.Category;
using Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Core
{
    public interface IProductService
    {
        // Query
        //Task<Product?> GetByIdAsync(int id);
        //Task<Product?> GetByIdWithCategoryAsync(int id);
        //Task<Product?> GetBySkuAsync(string sku);
        //Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids);

        Task<PagedResult<ProductListResult>> GetProductsAsync(DataTableRequest request);
        Task<List<SelectListItem>> GetCreateProductCategoriesAsync(CancellationToken cancellationToken = default);
        Task<List<SelectListItem>> GetCreateAttributesAsync(CancellationToken cancellationToken = default);
        //Task<ServiceResult<CreateProductRequest>> GetForEditAsync(int id, CancellationToken cancellationToken);
        Task<bool> ExistsBySKUProductAsync(string slug);
        Task<bool> ExistsBySlugProductAsync(string slug);
        Task<ServiceResult> MoveToDraftAsync(DeleteFormRequest request, CancellationToken cancellationToken);
        Task<ServiceResult<ProductStatus>> ToggleSuspendAsync(int productId, CancellationToken cancellationToken);
        //Task<ServiceResult<int>> UpdateStockStatusAsync(UpdateProductStockRequest request);

        //Task<IReadOnlyList<Product>> GetActiveAsync();

        //Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId);

        //Task<bool> ExistsAsync(int id);

        //Task<bool> ExistsSkuAsync(string sku, int? excludeId = null);


        //// CRUD
        Task<ServiceResult<int>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> UpdateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
        Task<ServiceResult<ProductUpdateResponse>> GetUpdateProductAsync(int id, CancellationToken cancellationToken = default);

        //Service Category Product
        Task<PagedResult<ProductCategoryListResult>> GetCategoryListAsync(DataTableRequest request);
        Task<EditProductCategoryRequest> GetCategoryIdAsync(int categoryId);
        Task<bool> ExistsBySlugCategoryAsync(string slug);
        Task<string> GenerateUniqueSlugProductAsync(string name);
        Task<string> GenerateUniqueSlugCategoryAsync(string name);
        Task<ServiceResult<int>> SaveCategoryAsync(EditProductCategoryRequest model, CancellationToken cancellationToken);
        Task<ServiceResult<int>> DeleteAsync(DeleteFormRequest request);
        //Task<ServiceResult<int>> CreateProductCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default);
    }
}
