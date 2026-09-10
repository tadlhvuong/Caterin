using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.DTOs.Identity;
using Shared.DTOs.Product;
using Shared.Enums;
using Shared.Requests;
using Shared.Requests.Product;
using Shared.Requests.Product.Category;
using Shared.Responses;
using Shared.Responses.Datatables;
using Shared.Responses.Product;
using AttributeEntity = Shared.Data.Entities.Product.Attribute;

namespace Shared.Interfaces.Core
{
    public interface IProductService
    {
        #region IProduct

        /// <summary>
        /// Show product list
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PagedResult<ProductListResult>> GetProductsAsync(ProductDataTableResquest request);
        /// <summary>
        /// Get category list for view _ProductForm 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<SelectListItem>> GetCreateProductCategoriesAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Get attribute list for view _ProductForm 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<SelectListItem>> GetCreateAttributesAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Generate unique slug product follow product name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<string> GenerateUniqueSlugProductAsync(string name);
        /// <summary>
        /// Change product type. use in view product list
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult<ProductStatus>> ToggleSuspendAsync(int productId, CancellationToken cancellationToken);
        /// <summary>
        /// Check product SKU exist
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        Task<bool> ExistsBySKUProductAsync(string slug);
        /// <summary>
        /// Check product slug exits
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        Task<bool> ExistsBySlugProductAsync(string slug);
        /// <summary>
        /// Create product
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult<int>> CreateAsync(ProductRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Get data product updated
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult<ProductResponse>> GetUpdateProductAsync(int id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Update product
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult<int>> UpdateAsync(ProductRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Soft delete  product
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult> MoveToDraftAsync(DeleteFormRequest request, CancellationToken cancellationToken);
        #endregion IProduct

        #region ICategory
        /// <summary>
        /// Get category list
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PagedResult<ProductCategoryListResult>> GetCategoryListAsync(DataTableRequest request);
        /// <summary>
        /// Get data follow id category
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        Task<EditProductCategoryRequest> GetCategoryIdAsync(int categoryId);
        /// <summary>
        /// Check category slug exits
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        Task<bool> ExistsBySlugCategoryAsync(string slug);
        /// <summary>
        /// Generate unique category slug
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<string> GenerateUniqueSlugCategoryAsync(string name);
        /// <summary>
        /// Create/Update category
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult<int>> SaveCategoryAsync(EditProductCategoryRequest request, CancellationToken cancellationToken);
        /// <summary>
        /// Soft Delete category
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ServiceResult<int>> DeleteCategoryAsync(DeleteFormRequest request);
        #endregion ICategory


        #region IAttribute
        /// <summary>
        /// Get Attribute list
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PagedResult<ProductAttributeListResult>> GetAttributeListAsync(DataTableRequest request);
        /// <summary>
        /// Get data follow id attribute
        /// </summary>
        /// <param name="attributeId"></param>
        /// <returns></returns>
        Task<AttributeEntity> GetAttributeIdAsync(int attributeId);
        /// <summary>
        /// Create/Update attribute
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult<int>> SaveAttributeAsync(AttributeEntity request, CancellationToken cancellationToken);
        /// <summary>
        /// Soft Delete attribute
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ServiceResult<int>> DeleteAttributeAsync(DeleteFormRequest request);
        #endregion IAttribute
    }
}
