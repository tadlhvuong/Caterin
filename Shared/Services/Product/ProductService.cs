using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Shared.Data.Context;
using Shared.Data.Entities.Catelog;
using Shared.Data.Entities.Media;
using Shared.Data.Entities.Product;
using Shared.DTOs.Identity;
using Shared.DTOs.Product;
using Shared.Enums;
using Shared.Interfaces.Core;
using Shared.Interfaces.Media;
using Shared.Requests;
using Shared.Requests.Product;
using Shared.Requests.Product.Category;
using Shared.Responses;
using Shared.Services.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Shared.Common.CommonHelper;
using ProductEntity = Shared.Data.Entities.Product.Product;

namespace Shared.Services.Product
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<OrderService> _logger;
        private readonly MediaStorageOptions _mediaStorage;
        private readonly IMediaService _mediaService;
        public ProductService(AppDbContext dbContext, IOptions<MediaStorageOptions> mediaStorage, IMediaService mediaService)
        {
            _dbContext = dbContext;
            _mediaService = mediaService;
            _mediaStorage = mediaStorage.Value;
        }

        public async Task<PagedResult<ProductListResult>> GetProductsAsync(DataTableRequest request)
        {
            var query = _dbContext.Products.AsNoTracking().AsQueryable();

            // =========================
            // TOTAL
            // =========================

            var totalCount = await query.CountAsync();


            // =========================
            // SEARCH
            // =========================

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(x => x.Name.Contains(search) || (x.Sku != null && x.Sku.Contains(search)));
            }


            // =========================
            // STATUS
            // =========================

            if (request.Status.HasValue && Enum.IsDefined(typeof(ProductStatus), request.Status.Value))
            {
                var status = (ProductStatus)request.Status.Value;
                query = query.Where(x => x.Status == status);
            }


            // =========================
            // STOCK
            // =========================

            if (request.Stock.HasValue)
            {
                if (request.Stock.Value == 1)
                {
                    // Còn hàng
                    query = query.Where(x => x.Variants.Any(v => v.InventoryStocks.Any(s => s.AvailableQuantity > 0)));
                }
                else if (request.Stock.Value == 0)
                {
                    // Hết hàng
                    query = query.Where(x => !x.Variants.Any(v =>
                            v.InventoryStocks.Any(s =>
                                s.AvailableQuantity > 0)));
                }
            }

            // =========================
            // FILTERED COUNT
            // =========================

            var filteredCount = await query.CountAsync();


            // =========================
            // SORT
            // =========================

            var sortColumn = request.SortColumn?.ToLower();

            query = sortColumn switch
            {
                "productname" or "name" =>
                    request.SortDirection == "desc"
                        ? query.OrderByDescending(x => x.Name)
                        : query.OrderBy(x => x.Name),

                "sku" =>
                    request.SortDirection == "desc"
                        ? query.OrderByDescending(x => x.Sku)
                        : query.OrderBy(x => x.Sku),

                "price" =>
                    request.SortDirection == "desc"
                        ? query.OrderByDescending(x => x.Price)
                        : query.OrderBy(x => x.Price),

                "status" =>
                    request.SortDirection == "desc"
                        ? query.OrderByDescending(x => x.Status)
                        : query.OrderBy(x => x.Status),

                _ =>
                    query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            };


            // =========================
            // PAGING + SELECT
            // =========================

            var items = await query.Skip(request.Start).Take(request.Length)
            .Select(x => new ProductListResult
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                ShortDescription = x.ShortDescription,
                ImageUrl = "Chưa rõ",

                CategoryId = x.CategoryId,
                CategoryName = x.Category != null
                    ? x.Category.Name
                    : string.Empty,

                Sku = x.Sku,
                Price = x.Price,

                // Tổng số lượng có thể bán
                Quantity = x.Variants
                .SelectMany(v => v.InventoryStocks)
                .Sum(s => s.AvailableQuantity),

                // Có ít nhất một variant còn hàng
                InStock = x.Variants
                .SelectMany(v => v.InventoryStocks)
                .Any(s => s.AvailableQuantity > 0),

                Status = x.Status,

                IsFeatured = x.IsFeatured,
                DisplayOrder = x.DisplayOrder,
                CreatedAt = x.CreatedAt
            }).ToListAsync();


            // =========================
            // RESULT
            // =========================

            return new PagedResult<ProductListResult>
            {
                Items = items,

                Page = request.Length > 0
                    ? request.Start / request.Length + 1
                    : 1,

                PageSize = request.Length,

                TotalCount = totalCount,

                FilteredCount = filteredCount
            };
        }
        public async Task<List<SelectListItem>> GetCreateProductCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.ProductCategories
                .AsNoTracking()
                .Where(x =>
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToListAsync(cancellationToken);
        }
        public async Task<List<SelectListItem>> GetCreateAttributesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Attributes
                .AsNoTracking()
                .OrderBy(x => x.CreatedAt)
                .Select(x => new SelectListItem
                {
                    Value = x.Code.ToString(),
                    Text = x.Name
                })
                .ToListAsync(cancellationToken);
        }

        #region Create product 
        public async Task<ServiceResult<int>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
        {
            var sku = request.SKU.Trim();

            // =========================================================
            // VALIDATE SKU
            // =========================================================

            var skuExists = await _dbContext.Products.AnyAsync(x => x.Sku == sku, cancellationToken);

            if (skuExists)
            {
                return ServiceResult<int>.Fail(
                    $"SKU '{sku}' đã tồn tại.");
            }


            // =========================================================
            // SLUG
            // =========================================================

            var slug = string.IsNullOrWhiteSpace(request.Slug)
                ? SlugHelper.Generate(request.Name)
                : SlugHelper.Generate(request.Slug);

            var slugExists = await _dbContext.Products.AnyAsync(x => x.Slug == slug, cancellationToken);

            if (slugExists)
            {
                slug = $"{slug}-{Guid.NewGuid():N}";
            }

            //        var productVariants = string.IsNullOrWhiteSpace(request.Variants) ? [] 
            //        : JsonSerializer.Deserialize<List<CreateProductVariantRequest>>(
            //    request.Variants
            //) ?? [];
            var productVariants =
    string.IsNullOrWhiteSpace(request.Variants)
        ? []
        : JsonSerializer.Deserialize<List<CreateProductVariantRequest>>(
            request.Variants,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
          ?? [];

            var status = request.Action switch
            {
                "publish" => ProductStatus.Active,
                "draft" => ProductStatus.Draft,
                _ => ProductStatus.Draft
            };
            // =========================================================
            // TRANSACTION
            // =========================================================

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // =====================================================
                // CREATE PRODUCT
                // =====================================================

                var product = new ProductEntity
                {
                    Sku = sku,
                    Name = request.Name.Trim(),
                    Slug = slug,

                    SeoTitle = request.MetaTitle?.Trim(),
                    SeoDescription = request.MetaDescription?.Trim(),
                    Description = request.Description,

                    CategoryId = request.CategoryId,
                    Price = productVariants is { Count: > 0 } ? null : request.Price,

                    Stock = productVariants is { Count: > 0 } ? null : request.Stock,

                    IsFeatured = request.IsFeatured,

                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                };


                await _dbContext.Products.AddAsync(product, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);


                // =====================================================
                // CREATE PRODUCT IMAGES
                // =====================================================

                if (request.ProductImages != null && request.ProductImages.Count > 0)
                {
                    await CreateProductImagesAsync(product, request.ProductImages, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }


                // =====================================================
                // CREATE TAGS
                // =====================================================
                await CreateProductTagsAsync(product, request.Tags, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // =====================================================
                // CREATE VARIANTS
                // =====================================================

                if (productVariants != null && productVariants.Count > 0)
                {
                    var variants = await CreateVariantsAsync(product, productVariants, cancellationToken);

                    await CreateVariantAttributesAsync(
                        variants,
                        productVariants,
                        cancellationToken);

                    await CreateVariantImagesAsync(
                        variants,
                        request.VariantImages,
                        cancellationToken);


                    await _dbContext.SaveChangesAsync(cancellationToken);

                }


                // =====================================================
                // COMMIT
                // =====================================================

                await transaction.CommitAsync(
                    cancellationToken);


                return ServiceResult<int>.Success(product.Id);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return ServiceResult<int>.Fail(
                    "Không thể lưu sản phẩm. Vui lòng thử lại.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return ServiceResult<int>.Fail(
                    "Đã xảy ra lỗi khi tạo sản phẩm.");
            }
        }
        private async Task CreateProductImagesAsync(ProductEntity product, List<CreateProductImageRequest>? images,
            CancellationToken cancellationToken = default)
        {
            // ==========================================
            // PRODUCT IMAGES
            // ==========================================

            if (images != null)
            {
                foreach (var image in images.OrderBy(x => x.DisplayOrder))
                {
                    if (image.File == null || image.File.Length == 0)
                        continue;

                    var mediaFile = await SaveMediaFileAsync(image.File, cancellationToken);

                    product.ProductMedias.Add(new ProductMedia
                    {
                        MediaFile = mediaFile,
                        DisplayOrder = image.DisplayOrder,
                        IsPrimary = image.IsPrimary
                    });
                }
            }

        }
        private async Task<MediaFile> SaveMediaFileAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(file.FileName);

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var relativeDirectory = Path.Combine(
                _mediaStorage.ProductFolder,
                DateTime.UtcNow.ToString("yyyy"),
                DateTime.UtcNow.ToString("MM"));

            var relativePath = Path.Combine(
                relativeDirectory,
                fileName);

            var physicalDirectory = Path.Combine(
                _mediaStorage.RootPath,
                relativeDirectory);

            Directory.CreateDirectory(physicalDirectory);

            var physicalPath = Path.Combine(
                _mediaStorage.RootPath,
                relativePath);

            await using var stream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await file.CopyToAsync(
                stream,
                cancellationToken);

            var mediaFile = new MediaFile
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                StoragePath = relativePath.Replace('\\', '/'),
                ContentType = file.ContentType,
                Size = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.MediaFiles.Add(mediaFile);

            return mediaFile;
        }

        private async Task<List<ProductVariant>> CreateVariantsAsync(ProductEntity product, ICollection<CreateProductVariantRequest>? requests,
        CancellationToken cancellationToken = default)
        {
            if (requests == null || requests.Count == 0)
                return [];

            var variantRequests = requests.ToList();

            // ==========================================
            // VALIDATE SKU
            // ==========================================

            var duplicateSkus = variantRequests.GroupBy(x => x.SKU.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1).Select(x => x.Key).ToList();

            if (duplicateSkus.Count > 0)
            {
                throw new InvalidOperationException(
                    $"SKU variant bị trùng: {string.Join(", ", duplicateSkus)}");
            }

            var skus = variantRequests
                .Select(x => x.SKU.Trim())
                .ToList();

            var existingSkus = await _dbContext.ProductVariants
                .Where(x => skus.Contains(x.Sku))
                .Select(x => x.Sku)
                .ToListAsync(cancellationToken);

            if (existingSkus.Count > 0)
            {
                throw new InvalidOperationException(
                    $"SKU variant đã tồn tại: {string.Join(", ", existingSkus)}");
            }

            // ==========================================
            // CREATE VARIANTS
            // ==========================================

            var variants = new List<ProductVariant>();

            for (var i = 0; i < variantRequests.Count; i++)
            {
                var request = variantRequests[i];

                var variant = new ProductVariant
                {
                    Product = product,

                    Name = BuildVariantName(request.Options),

                    Sku = request.SKU.Trim(),

                    Price = request.Price,

                    // Stock không lưu trực tiếp vào ProductVariant
                    // mà sẽ xử lý qua InventoryStock

                    IsDefault = i == 0,

                    IsActive = true,

                    DisplayOrder = i,

                    CreatedAt = DateTime.UtcNow,

                    VariantAttributes = [],

                    VariantMedias = []
                };

                product.Variants.Add(variant);

                variants.Add(variant);
            }

            return variants;
        }
        private async Task CreateVariantAttributesAsync(IEnumerable<ProductVariant> variants, ICollection<CreateProductVariantRequest> requests,
        CancellationToken cancellationToken = default)
        {
            var variantList = variants.ToList();
            var requestList = requests.ToList();

            if (variantList.Count == 0)
                return;

            if (variantList.Count != requestList.Count)
            {
                throw new InvalidOperationException(
                    "Số lượng variant không khớp với số lượng request.");
            }

            // ==========================================
            // LOAD ATTRIBUTES + VALUES
            // ==========================================

            var attributeNames = requestList
                .SelectMany(x => x.Options.Keys)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (attributeNames.Count == 0)
                return;

            var attributes = await _dbContext.Attributes
                .Include(x => x.Values)
                .Where(x => attributeNames.Contains(x.Code))
                .ToListAsync(cancellationToken);

            // ==========================================
            // CREATE VARIANT ATTRIBUTES
            // ==========================================

            for (var i = 0; i < variantList.Count; i++)
            {
                var variant = variantList[i];
                var request = requestList[i];

                foreach (var option in request.Options)
                {
                    var attributeName = option.Key.Trim();
                    var valueName = option.Value.Trim();

                    if (string.IsNullOrWhiteSpace(attributeName) ||
                        string.IsNullOrWhiteSpace(valueName))
                    {
                        continue;
                    }

                    // ------------------------------
                    // FIND ATTRIBUTE
                    // ------------------------------

                    var attribute = attributes.FirstOrDefault(x =>
                        string.Equals(
                            x.Code,
                            attributeName,
                            StringComparison.OrdinalIgnoreCase));

                    if (attribute == null)
                    {
                        throw new InvalidOperationException(
                            $"Không tìm thấy Attribute '{attributeName}'.");
                    }

                    // ------------------------------
                    // FIND ATTRIBUTE VALUE
                    // ------------------------------

                    var attributeValue = attribute.Values.FirstOrDefault(x =>
                        string.Equals(
                            x.Value,
                            valueName,
                            StringComparison.OrdinalIgnoreCase));

                    if (attributeValue == null)
                    {
                        attributeValue = new AttributeValue
                        {
                            Attribute = attribute,
                            Value = valueName,
                            CreatedAt = DateTime.UtcNow
                        };

                        attribute.Values.Add(attributeValue);
                    }

                    variant.VariantAttributes.Add(
                        new VariantAttribute
                        {
                            ProductVariant = variant,
                            AttributeValue = attributeValue
                        });
                }
            }
        }
        private async Task CreateVariantImagesAsync(IEnumerable<ProductVariant> variants, IEnumerable<CreateProductVariantImageRequest>? variantImages,
        CancellationToken cancellationToken = default)
        {
            if (variantImages == null)
                return;

            var variantList = variants.ToList();

            if (variantList.Count == 0)
                return;

            foreach (var imageRequest in variantImages)
            {
                var key = imageRequest.Key;
                var file = imageRequest.File;

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (file == null || file.Length == 0)
                    continue;

                // ==========================================
                // PARSE KEY
                // ==========================================

                var parts = key.Split(
                    ':',
                    2,
                    StringSplitOptions.TrimEntries);

                if (parts.Length != 2)
                    continue;

                var attributeName = parts[0];
                var attributeValueName = parts[1];

                if (string.IsNullOrWhiteSpace(attributeName) ||
                    string.IsNullOrWhiteSpace(attributeValueName))
                {
                    continue;
                }

                // ==========================================
                // FIND MATCHING VARIANTS
                // ==========================================

                var matchingVariants = variantList
                    .Where(variant =>
                        variant.VariantAttributes.Any(va =>
                            string.Equals(
                                va.AttributeValue.Attribute.Code,
                                attributeName,
                                StringComparison.OrdinalIgnoreCase)
                            &&
                            string.Equals(
                                va.AttributeValue.Value,
                                attributeValueName,
                                StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (matchingVariants.Count == 0)
                    continue;

                // ==========================================
                // FIND ATTRIBUTE VALUE
                // ==========================================

                var attributeValue = matchingVariants
                    .SelectMany(x => x.VariantAttributes)
                    .Select(x => x.AttributeValue)
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Attribute.Code,
                            attributeName,
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        string.Equals(
                            x.Value,
                            attributeValueName,
                            StringComparison.OrdinalIgnoreCase));

                if (attributeValue == null)
                    continue;

                // ==========================================
                // SAVE MEDIA FILE ONCE
                // ==========================================

                var mediaFile = await SaveMediaFileAsync(
                    file,
                    cancellationToken);

                // ==========================================
                // CREATE PRODUCT VARIANT MEDIA
                // ==========================================

                foreach (var variant in matchingVariants)
                {
                    variant.VariantMedias.Add(
                        new ProductVariantMedia
                        {
                            ProductVariant = variant,
                            AttributeValue = attributeValue,
                            MediaFile = mediaFile,
                            DisplayOrder = 0
                        });
                }
            }
        }

        private async Task CreateProductTagsAsync(ProductEntity product, string? tags, CancellationToken cancellationToken)
        { // ========================================================= // 1. PARSE + NORMALIZE TAG VALUES // =========================================================
            if (string.IsNullOrWhiteSpace(tags)) { return; }
            var tagValues = JsonSerializer.Deserialize<List<string>>(tags) ?? [];
            var tagNames = tagValues.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (tagNames.Count == 0) { return; }
            // ========================================================= // 2. GENERATE SLUG // =========================================================
            var tagRequests = tagNames.Select(name => new { Name = name, Slug = SlugHelper.Generate(name) }).Where(x => !string.IsNullOrWhiteSpace(x.Slug)).GroupBy(x => x.Slug, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
            if (tagRequests.Count == 0) { return; }
            // ========================================================= // 3. GET EXISTING TAGS // =========================================================
            var slugs = tagRequests.Select(x => x.Slug).ToList();
            var existingTags = await _dbContext.ProductTags.Where(x => slugs.Contains(x.Slug)).ToListAsync(cancellationToken);
            var tagBySlug = existingTags.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);
            // ========================================================= // 4. CREATE NEW TAGS // =========================================================
            var newTags = new List<ProductTag>();
            foreach (var request in tagRequests)
            {
                if (tagBySlug.ContainsKey(request.Slug)) { continue; }
                var tag = new ProductTag { Name = request.Name, Slug = request.Slug, IsActive = true, NoIndex = true, CreatedAt = DateTime.UtcNow };
                newTags.Add(tag);
                // Add ngay vào dictionary để có thể // sử dụng cùng với existing tags bên dưới.
                tagBySlug[request.Slug] = tag;
            }
            if (newTags.Count > 0)
            {
                await _dbContext.ProductTags.AddRangeAsync(newTags, cancellationToken);
            }
            // ========================================================= // 5. CREATE PRODUCT TAG MAPPINGS // =========================================================
            var mappings = tagRequests.Select(request => tagBySlug[request.Slug]).Select(tag => new ProductTagMapping { ProductId = product.Id, Tag = tag }).ToList();
            if (mappings.Count == 0) { return; }
            await _dbContext.ProductTagMappings.AddRangeAsync(mappings, cancellationToken);
        }

        //join all value variant -> build name product variant
        private static string BuildVariantName(Dictionary<string, string> options)
        {
            return string.Join(
                " - ",
                options.Values);
        }

        #endregion Create product 

        #region Update product
        public async Task<ServiceResult<int>> UpdateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
        {
            // =========================================================
            // VALIDATE PRODUCT ID
            // =========================================================

            if (request.Id <= 0)
            {
                return ServiceResult<int>.Fail( "Sản phẩm không hợp lệ.");
            }

            // =========================================================
            // LOAD PRODUCT
            // =========================================================

            var product = await _dbContext.Products
                .Include(x => x.ProductMedias).ThenInclude(x => x.MediaFile)
                .Include(x => x.Variants).ThenInclude(x => x.VariantAttributes).ThenInclude(x => x.AttributeValue).ThenInclude(x => x.Attribute)
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id,
                    cancellationToken);

            if (product == null)
            {
                return ServiceResult<int>.Fail(
                    "Không tìm thấy sản phẩm.");
            }

            // =========================================================
            // VALIDATE
            // =========================================================

            var validationResult = await ValidateUpdateProductAsync(
                    product,
                    request,
                    cancellationToken);

            if (!validationResult.Succeeded)
            {
                return validationResult;
            }

            // =========================================================
            // PARSE OPTIONS
            // =========================================================

            var options = DeserializeOptions(request.Options);

            if (options == null)
            {
                return ServiceResult<int>.Fail(
                    "Dữ liệu option không hợp lệ.");
            }

            // =========================================================
            // PARSE VARIANTS
            // =========================================================

            var variants = DeserializeVariants(request.Variants);

            if (variants == null)
            {
                return ServiceResult<int>.Fail(
                    "Dữ liệu variant không hợp lệ.");
            }

            // =========================================================
            // TRANSACTION
            // =========================================================

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // =====================================================
                // UPDATE BASIC PRODUCT
                // =====================================================

                UpdateBasicProduct(
                    product,
                    request);

                // =====================================================
                // UPDATE PRODUCT IMAGES
                // =====================================================

                await UpdateProductImagesAsync(
                    product,
                    request.ProductImages,
                    cancellationToken);

                // =====================================================
                // UPDATE PRODUCT TAGS
                // =====================================================

                await UpdateTagsAsync(product, request.Tags, cancellationToken);

                // =====================================================
                // UPDATE VARIANTS
                // =====================================================

                await UpdateVariantsAsync(
                    product,
                    options,
                    variants,
                    cancellationToken);

                // =====================================================
                // UPDATE VARIANT IMAGES
                // =====================================================

                await UpdateVariantImagesAsync(
                    product,
                    request.VariantImages,
                    cancellationToken);

                // =====================================================
                // SAVE
                // =====================================================

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                // =====================================================
                // COMMIT
                // =====================================================

                await transaction.CommitAsync(
                    cancellationToken);

                return ServiceResult<int>.Success(
                    product.Id,
                    "Cập nhật sản phẩm thành công.");
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }
        public async Task<ServiceResult<ProductUpdateResponse>> GetUpdateProductAsync(int id,
        CancellationToken cancellationToken = default)
        {
            // =========================================================
            // LOAD PRODUCT
            // =========================================================
            var product = new ProductEntity();
            try
            {
                product = await _dbContext.Products
                    .AsNoTracking()

                    // Product Images
                    .Include(x => x.ProductMedias)
                        .ThenInclude(x => x.MediaFile)
                // Product Tags
                .Include(x => x.ProductTagMappings)
                    .ThenInclude(x => x.Tag)

                    // Variants -> Attributes
                    .Include(x => x.Variants)
                        .ThenInclude(x => x.VariantAttributes)
                            .ThenInclude(x => x.AttributeValue)
                                .ThenInclude(x => x.Attribute)

                    // Variants -> Images
                    .Include(x => x.Variants)
                        .ThenInclude(x => x.VariantMedias)
                            .ThenInclude(x => x.MediaFile)
                              .Include(x => x.Variants)
        .ThenInclude(x => x.VariantMedias)
            .ThenInclude(x => x.AttributeValue)
                .ThenInclude(x => x.Attribute)
                    .FirstOrDefaultAsync(
                        x => x.Id == id,
                        cancellationToken);
                
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductUpdateResponse>.Fail(
                   ex.Message.ToString());
               
            }

            if (product == null)
            {
                return ServiceResult<ProductUpdateResponse>.Fail(
                    $"Không tìm thấy sản phẩm có Id = {id}.");
            }

            // =========================================================
            // RESPONSE
            // =========================================================

            var response = new ProductUpdateResponse
            {
                Id = product.Id,

                SKU = product.Sku,

                Name = product.Name,

                Slug = product.Slug,

                CategoryId = product.CategoryId,

                Tags = product.ProductTagMappings
    .Where(x => x.Tag != null)
    .Select(x => x.Tag.Name)
    .ToList(),
                Price = product.Price,


                Weight = product.Weight,

                IsFeatured = product.IsFeatured,


                Description = product.Description,


                Status = product.Status,
            };

            // =========================================================
            // PRODUCT IMAGES
            // =========================================================

            response.Images = product.ProductMedias
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new ProductImageResponse
                {
                    Id = x.Id,

                    Url = BuildMediaUrl( x.MediaFile),

                    DisplayOrder =
                        x.DisplayOrder,

                    IsPrimary =
                        x.IsPrimary
                })
                .ToList();

            // =========================================================
            // OPTIONS
            // =========================================================

            response.Options = product.Variants
                .SelectMany(x => x.VariantAttributes)
                .Where(x =>
                    x.AttributeValue != null &&
                    x.AttributeValue.Attribute != null)
                .GroupBy(x => new
                {
                    AttributeId =
                        x.AttributeValue.AttributeId,

                    Code =
                        x.AttributeValue.Attribute.Code,

                    Name =
                        x.AttributeValue.Attribute.Name
                })
                .Select(group => new CreateProductOptionResponse
                {
                    Name =
                        group.Key.Code ??
                        group.Key.Name,

                    Values = group
                        .Select(x => x.AttributeValue.Value)
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase)
                        .ToList()
                })
                .ToList();

            // =========================================================
            // VARIANTS
            // =========================================================

            response.Variants = product.Variants
                .OrderBy(x => x.DisplayOrder)
                .Select(variant =>
                    new ProductVariantResponse
                    {
                        Id = variant.Id,

                        SKU = variant.Sku,

                        Price = variant.Price,

                        IsDefault =
                            variant.IsDefault,

                        IsActive =
                            variant.IsActive,

                        Options =
                            variant.VariantAttributes
                                .Where(x =>
                                    x.AttributeValue != null &&
                                    x.AttributeValue.Attribute != null)
                                .ToDictionary(
                                    x =>
                                        x.AttributeValue.Attribute.Code,

                                    x =>
                                        x.AttributeValue.Value,

                                    StringComparer.OrdinalIgnoreCase)
                    })
                .ToList();

            // =========================================================
            // VARIANT IMAGES
            // =========================================================

            response.VariantImages = product.Variants
                .SelectMany(x => x.VariantMedias)
                .Where(x =>
                    x.MediaFile != null &&
                    x.AttributeValue != null &&
                    x.AttributeValue.Attribute != null)
                .Select(x => new ProductVariantImageResponse
                {
                    Key =
                        $"{x.AttributeValue.Attribute.Code}:{x.AttributeValue.Value}",

                    Id = x.Id,

                    Url = BuildMediaUrl(
                        x.MediaFile)
                })
                .ToList();

            // =========================================================
            // SUCCESS
            // =========================================================

            return ServiceResult<ProductUpdateResponse>.Success(
                response);
        }
        private string BuildMediaUrl(MediaFile mediaFile)
        {
            return "/" + mediaFile.StoragePath
                .Replace("\\", "/");
        }
        private static List<CreateProductOptionRequest> DeserializeOptions(string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<
                    List<CreateProductOptionRequest>
                >(
                    optionsJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? [];
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "Dữ liệu options không hợp lệ.",
                    ex);
            }
        }
        private static List<CreateProductVariantRequest> DeserializeVariants(string? variantsJson)
        {
            if (string.IsNullOrWhiteSpace(variantsJson))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<
                    List<CreateProductVariantRequest>
                >(
                    variantsJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? [];
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "Dữ liệu variant không hợp lệ.", ex);
            }
        }
        private static void UpdateBasicProduct(ProductEntity product,CreateProductRequest request)
        {
            var status = request.Action switch
            {
                "publish" => ProductStatus.Active,
                "draft" => ProductStatus.Draft,
                _ => ProductStatus.Draft
            };
            product.Sku = request.SKU.Trim();

            product.Name = request.Name.Trim();

            product.Slug = request.Slug.Trim();

            product.CategoryId = request.CategoryId;

            product.Price = request.Price;

            product.Stock = request.Stock;

            product.Weight = request.Weight;

            product.IsFeatured = request.IsFeatured;

            product.Status = status;

            product.Description = request.Description;
        }
        private async Task UpdateProductImagesAsync(ProductEntity product,List<CreateProductImageRequest> requests,
        CancellationToken cancellationToken)
        {
            var existingImages =
                product.ProductMedias.ToList();

            var requestIds = requests .Where(x => x.Id.HasValue)
                            .Select(x => x.Id!.Value) .ToHashSet();

            // =========================================================
            // DELETE REMOVED IMAGES
            // =========================================================

            foreach (var existingImage in existingImages)
            {
                if (requestIds.Contains(existingImage.Id))
                {
                    continue;
                }

                var mediaFile =
                    existingImage.MediaFile;

                product.ProductMedias.Remove(
                    existingImage);

                _dbContext.ProductMedias.Remove(
                    existingImage);

                if (mediaFile != null)
                {
                    await _mediaService.DeleteAsync(
                        mediaFile,
                        cancellationToken);
                }
            }

            // =========================================================
            // UPDATE / CREATE
            // =========================================================

            foreach (var request in requests)
            {
                // =====================================================
                // EXISTING
                // =====================================================

                if (request.Id.HasValue)
                {
                    var existingImage =
                        existingImages.FirstOrDefault(
                            x => x.Id == request.Id.Value);

                    if (existingImage == null)
                    {
                        throw new InvalidOperationException(
                            $"ProductMedia {request.Id.Value} không thuộc sản phẩm.");
                    }

                    existingImage.DisplayOrder =
                        request.DisplayOrder;

                    existingImage.IsPrimary =
                        request.IsPrimary;

                    // -------------------------------------------------
                    // REPLACE FILE
                    // -------------------------------------------------

                    if (request.File != null)
                    {
                        var oldMedia =
                            existingImage.MediaFile;

                        var newMedia =
                            await _mediaService.UploadAsync(
                                request.File,
                                _mediaStorage.ProductFolder,
                                cancellationToken);

                        existingImage.MediaFile =
                            newMedia;

                        existingImage.MediaFileId =
                            newMedia.Id;

                        if (oldMedia != null)
                        {
                            await _mediaService.DeleteAsync(
                                oldMedia,
                                cancellationToken);
                        }
                    }

                    continue;
                }

                // =====================================================
                // NEW IMAGE
                // =====================================================

                if (request.File == null)
                {
                    continue;
                }

                var media =
                    await _mediaService.UploadAsync(
                        request.File,
                        _mediaStorage.ProductFolder,
                        cancellationToken);

                var productMedia = new ProductMedia
                {
                    ProductId =
                        product.Id,

                    MediaFileId =
                        media.Id,

                    MediaFile =
                        media,

                    DisplayOrder =
                        request.DisplayOrder,

                    IsPrimary =
                        request.IsPrimary
                };

                product.ProductMedias.Add(
                    productMedia);
            }
        }
        private async Task UpdateVariantsAsync(ProductEntity product, List<CreateProductOptionRequest> options, 
            List<CreateProductVariantRequest> requests, CancellationToken cancellationToken)
        {
            // =========================================================
            // VALIDATE
            // =========================================================

            ValidateVariantRequests(requests);

            // =========================================================
            // NO VARIANT
            // =========================================================

            if (requests.Count == 0)
            {
                foreach (var variant in product.Variants)
                {
                    variant.IsActive = false;
                    variant.IsDefault = false;
                }

                return;
            }

            // =========================================================
            // EXISTING VARIANTS
            // =========================================================

            var existingVariants =
                product.Variants.ToList();

            var requestIds =
                requests
                    .Where(x => x.Id.HasValue)
                    .Select(x => x.Id!.Value)
                    .ToHashSet();

            // =========================================================
            // DETERMINE MAIN OPTION
            // =========================================================

            var mainOption =
                options.FirstOrDefault();

            if (mainOption == null ||
                string.IsNullOrWhiteSpace(mainOption.Name))
            {
                throw new InvalidOperationException(
                    "Không xác định được option chính của variant.");
            }

            var mainOptionName =
                mainOption.Name.Trim();

            // =========================================================
            // DETERMINE MAIN VARIANT VALUES
            // =========================================================

            var requestedMainValues =
                requests
                    .Select(x =>
                        GetOptionValue(
                            x.Options,
                            mainOptionName))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            // =========================================================
            // UPDATE / CREATE REQUESTED VARIANTS
            // =========================================================

            foreach (var request in requests)
            {
                if (request.Id.HasValue)
                {
                    await UpdateExistingVariantAsync(
                        product,
                        request,
                        cancellationToken);

                    continue;
                }

                await CreateNewVariantAsync(
                    product,
                    request,
                    cancellationToken);
            }

            // =========================================================
            // REMOVE VARIANTS NO LONGER EXIST
            // =========================================================

            foreach (var variant in existingVariants)
            {
                if (requestIds.Contains(variant.Id))
                {
                    continue;
                }

                var mainValue =
                    GetMainVariantValue(
                        variant,
                        mainOptionName);

                if (string.IsNullOrWhiteSpace(mainValue))
                {
                    variant.IsActive = false;
                    variant.IsDefault = false;

                    continue;
                }

                if (!requestedMainValues.Contains(mainValue))
                {
                    await DeactivateVariantGroupAsync(
                        product,
                        mainOptionName,
                        mainValue,
                        cancellationToken);
                }
                else
                {
                    variant.IsActive = false;
                    variant.IsDefault = false;
                }
            }

            // =========================================================
            // DEFAULT VARIANT
            // =========================================================

            NormalizeDefaultVariant(product);
        }
        private static string? GetOptionValue(
    Dictionary<string, string> options,
    string optionName)
        {
            if (options == null ||
                options.Count == 0)
            {
                return null;
            }

            var option =
                options.FirstOrDefault(x =>
                    string.Equals(
                        x.Key.Trim(),
                        optionName,
                        StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(option.Key)
                ? null
                : option.Value?.Trim();
        }
        private static string? GetMainVariantValue(
    ProductVariant variant,
    string mainOptionName)
        {
            var attributeValue =
                variant.VariantAttributes
                    .Select(x => x.AttributeValue)
                    .FirstOrDefault(x =>
                        x.Attribute != null &&
                        (
                            string.Equals(
                                x.Attribute.Code,
                                mainOptionName,
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            string.Equals(
                                x.Attribute.Name,
                                mainOptionName,
                                StringComparison.OrdinalIgnoreCase)
                        ));

            return attributeValue?.Value?.Trim();
        }
        private async Task DeactivateVariantGroupAsync(
     ProductEntity product,
     string mainOptionName,
     string mainValue,
     CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(mainOptionName) ||
                string.IsNullOrWhiteSpace(mainValue))
            {
                return;
            }

            mainOptionName = mainOptionName.Trim();
            mainValue = mainValue.Trim();

            // =========================================================
            // FIND ATTRIBUTE
            // =========================================================

            var attribute = await _dbContext.Attributes
                .FirstOrDefaultAsync(
                    x => x.Code == mainOptionName ||
                         x.Name == mainOptionName,
                    cancellationToken);

            if (attribute == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy Attribute '{mainOptionName}'.");
            }

            // =========================================================
            // FIND ATTRIBUTE VALUE
            // =========================================================

            var attributeValue =
                await _dbContext.AttributeValues
                    .FirstOrDefaultAsync(
                        x =>
                            x.AttributeId == attribute.Id &&
                            x.Value == mainValue,
                        cancellationToken);

            if (attributeValue == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy AttributeValue '{mainValue}'.");
            }

            // =========================================================
            // FIND VARIANT GROUP
            //
            // Color:red
            // Color:red + Size:S
            // Color:red + Size:M
            //
            // đều thuộc cùng group Color:red
            // =========================================================

            var groupVariants = product.Variants
                .Where(x =>
                    x.VariantAttributes.Any(va =>
                        va.AttributeValueId ==
                        attributeValue.Id))
                .ToList();

            if (groupVariants.Count == 0)
            {
                return;
            }

            // =========================================================
            // DEACTIVATE ALL VARIANTS IN GROUP
            // =========================================================

            foreach (var variant in groupVariants)
            {
                variant.IsActive = false;
                variant.IsDefault = false;
            }

            // =========================================================
            // DELETE GROUP IMAGES
            // =========================================================

            var variantIds = groupVariants
                .Select(x => x.Id)
                .ToHashSet();

            var images = await _dbContext.ProductVariantMedias
                .Include(x => x.MediaFile)
                .Where(x =>
                    variantIds.Contains(
                        x.ProductVariantId))
                .ToListAsync(cancellationToken);

            foreach (var image in images)
            {
                var mediaFile = image.MediaFile;

                _dbContext.ProductVariantMedias.Remove(image);

                if (mediaFile != null)
                {
                    _dbContext.MediaFiles.Remove(mediaFile);

                    await _mediaService.DeleteAsync(
                        mediaFile,
                        cancellationToken);
                }
            }
        }
        private static void ValidateVariantRequests(
    List<CreateProductVariantRequest> requests)
        {
            if (requests == null || requests.Count == 0)
            {
                return;
            }

            // =========================================================
            // VALIDATE EACH VARIANT
            // =========================================================

            foreach (var request in requests)
            {
                if (request == null)
                {
                    throw new InvalidOperationException(
                        "Dữ liệu variant không hợp lệ.");
                }

                // -----------------------------------------------------
                // SKU
                // -----------------------------------------------------

                if (string.IsNullOrWhiteSpace(request.SKU))
                {
                    throw new InvalidOperationException(
                        "SKU variant không được để trống.");
                }

                // -----------------------------------------------------
                // PRICE
                // -----------------------------------------------------

                if (request.Price < 0)
                {
                    throw new InvalidOperationException(
                        $"Giá của variant '{request.SKU}' không hợp lệ.");
                }

                // -----------------------------------------------------
                // STOCK
                // -----------------------------------------------------

                if (request.Stock < 0)
                {
                    throw new InvalidOperationException(
                        $"Tồn kho của variant '{request.SKU}' không hợp lệ.");
                }

                // -----------------------------------------------------
                // OPTIONS
                // -----------------------------------------------------

                if (request.Options == null ||
                    request.Options.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Variant '{request.SKU}' phải có option.");
                }

                foreach (var option in request.Options)
                {
                    if (string.IsNullOrWhiteSpace(option.Key))
                    {
                        throw new InvalidOperationException(
                            $"Variant '{request.SKU}' có tên option không hợp lệ.");
                    }

                    if (string.IsNullOrWhiteSpace(option.Value))
                    {
                        throw new InvalidOperationException(
                            $"Variant '{request.SKU}' có giá trị option không hợp lệ.");
                    }
                }
            }

            // =========================================================
            // DUPLICATE SKU
            // =========================================================

            var duplicateSku = requests
     .Where(x => !string.IsNullOrWhiteSpace(x.SKU))
     .GroupBy(
         x => x.SKU.Trim(),
         StringComparer.OrdinalIgnoreCase)
     .FirstOrDefault(x => x.Count() > 1);

            if (duplicateSku != null)
            {
                throw new InvalidOperationException(
                    $"SKU variant '{duplicateSku.Key}' bị trùng.");
            }

            // =========================================================
            // DUPLICATE VARIANT ID
            // =========================================================

            var duplicateIds =
                requests
                    .Where(x => x.Id.HasValue)
                    .GroupBy(x => x.Id!.Value)
                    .FirstOrDefault(
                        x => x.Count() > 1);

            if (duplicateIds != null)
            {
                throw new InvalidOperationException(
                    $"Variant ID '{duplicateIds.Key}' bị trùng.");
            }
        }
        private async Task CreateNewVariantAsync(
    ProductEntity product,
    CreateProductVariantRequest request,
    CancellationToken cancellationToken)
        {
            var variant = new ProductVariant
            {
                ProductId = product.Id,

                Name = BuildVariantName(
                    request.Options),

                Sku = request.SKU.Trim(),

                Price = request.Price,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            product.Variants.Add(variant);

            await CreateVariantAttributesForVariantAsync(
                variant,
                request.Options,
                cancellationToken);
        }
        private async Task CreateVariantAttributesForVariantAsync(
     ProductVariant variant,
     Dictionary<string, string> options,
     CancellationToken cancellationToken = default)
        {
            if (options == null || options.Count == 0)
            {
                return;
            }

            var attributeNames = options.Keys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (attributeNames.Count == 0)
            {
                return;
            }

            var attributes = await _dbContext.Attributes
                .Include(x => x.Values)
                .Where(x => attributeNames.Contains(x.Code))
                .ToListAsync(cancellationToken);

            foreach (var option in options)
            {
                var attributeName = option.Key?.Trim();
                var valueName = option.Value?.Trim();

                if (string.IsNullOrWhiteSpace(attributeName) ||
                    string.IsNullOrWhiteSpace(valueName))
                {
                    continue;
                }

                var attribute = attributes.FirstOrDefault(x =>
                    string.Equals(
                        x.Code,
                        attributeName,
                        StringComparison.OrdinalIgnoreCase));

                if (attribute == null)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy Attribute '{attributeName}'.");
                }

                var attributeValue = attribute.Values.FirstOrDefault(x =>
                    string.Equals(
                        x.Value,
                        valueName,
                        StringComparison.OrdinalIgnoreCase));

                if (attributeValue == null)
                {
                    attributeValue = new AttributeValue
                    {
                        Attribute = attribute,
                        Value = valueName,
                        CreatedAt = DateTime.UtcNow
                    };

                    attribute.Values.Add(attributeValue);
                }

                variant.VariantAttributes.Add(
                    new VariantAttribute
                    {
                        ProductVariant = variant,
                        AttributeValue = attributeValue
                    });
            }
        }
        private async Task UpdateVariantAttributesAsync(
    ProductVariant variant,
    Dictionary<string, string> options,
    CancellationToken cancellationToken)
        {
            // =========================================================
            // REMOVE OLD VARIANT ATTRIBUTES
            // =========================================================

            var existingAttributes =
                variant.VariantAttributes.ToList();

            if (existingAttributes.Count > 0)
            {
                _dbContext.VariantAttributes.RemoveRange(
                    existingAttributes);
            }

            // =========================================================
            // NO OPTIONS
            // =========================================================

            if (options == null || options.Count == 0)
            {
                return;
            }

            // =========================================================
            // LOAD ATTRIBUTES
            // =========================================================

            var attributeNames = options.Keys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (attributeNames.Count == 0)
            {
                return;
            }

            var attributes =
                await _dbContext.Attributes
                    .Include(x => x.Values)
                    .Where(x =>
                        attributeNames.Contains(x.Code))
                    .ToListAsync(cancellationToken);

            // =========================================================
            // CREATE NEW VARIANT ATTRIBUTES
            // =========================================================

            foreach (var option in options)
            {
                var attributeName =
                    option.Key.Trim();

                var valueName =
                    option.Value.Trim();

                if (string.IsNullOrWhiteSpace(attributeName) ||
                    string.IsNullOrWhiteSpace(valueName))
                {
                    continue;
                }

                // -----------------------------------------------------
                // FIND ATTRIBUTE
                // -----------------------------------------------------

                var attribute =
                    attributes.FirstOrDefault(x =>
                        string.Equals(
                            x.Code,
                            attributeName,
                            StringComparison.OrdinalIgnoreCase));

                if (attribute == null)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy Attribute '{attributeName}'.");
                }

                // -----------------------------------------------------
                // FIND ATTRIBUTE VALUE
                // -----------------------------------------------------

                var attributeValue =
                    attribute.Values.FirstOrDefault(x =>
                        string.Equals(
                            x.Value,
                            valueName,
                            StringComparison.OrdinalIgnoreCase));

                // -----------------------------------------------------
                // CREATE ATTRIBUTE VALUE
                // -----------------------------------------------------

                if (attributeValue == null)
                {
                    attributeValue = new AttributeValue
                    {
                        Attribute = attribute,
                        Value = valueName,
                        CreatedAt = DateTime.UtcNow
                    };

                    attribute.Values.Add(
                        attributeValue);
                }

                // -----------------------------------------------------
                // CREATE VARIANT ATTRIBUTE
                // -----------------------------------------------------

                variant.VariantAttributes.Add(
                    new VariantAttribute
                    {
                        ProductVariant = variant,
                        AttributeValue = attributeValue
                    });
            }
        }
        private Task UpdateExistingVariantAsync(
    ProductEntity product,
    CreateProductVariantRequest request,
    CancellationToken cancellationToken)
        {
            if (!request.Id.HasValue)
            {
                throw new InvalidOperationException(
                    "Variant ID không hợp lệ.");
            }

            var variant =
                product.Variants
                    .FirstOrDefault(
                        x => x.Id == request.Id.Value);

            if (variant == null)
            {
                throw new InvalidOperationException(
                    $"Variant ID {request.Id.Value} không thuộc sản phẩm.");
            }

            variant.Name =
                BuildVariantName(request.Options);

            variant.Sku =
                request.SKU.Trim();

            variant.Price =
                request.Price;

            variant.IsActive = true;

            return Task.CompletedTask;
        }
        private static void NormalizeDefaultVariant(
     ProductEntity product)
        {
            var activeVariants =
                product.Variants
                    .Where(x => x.IsActive)
                    .ToList();

            if (activeVariants.Count == 0)
            {
                throw new InvalidOperationException(
                    "Sản phẩm phải có ít nhất một variant hoạt động.");
            }

            var mainVariants =
                activeVariants
                    .Where(x =>
                        x.VariantAttributes.Count == 1)
                    .ToList();

            var candidates =
                mainVariants.Count > 0
                    ? mainVariants
                    : activeVariants;

            var defaults =
                candidates
                    .Where(x => x.IsDefault)
                    .ToList();

            if (defaults.Count == 0)
            {
                candidates[0].IsDefault = true;
                return;
            }

            var first = defaults[0];

            foreach (var variant in defaults.Skip(1))
            {
                variant.IsDefault = false;
            }

            first.IsDefault = true;
        }
        private async Task UpdateVariantImagesAsync(
    ProductEntity product,
    List<CreateProductVariantImageRequest> requests,
    CancellationToken cancellationToken)
        {
            var existingImages =
                await GetExistingVariantImagesAsync(
                    product,
                    cancellationToken);

            var requestIds =
                requests
                    .Where(x => x.Id.HasValue)
                    .Select(x => x.Id!.Value)
                    .ToHashSet();

            // =========================================================
            // DELETE REMOVED
            // =========================================================

            foreach (var image in existingImages)
            {
                if (requestIds.Contains(image.Id))
                {
                    continue;
                }

                await DeleteVariantImageAsync(
                    image,
                    cancellationToken);
            }

            // =========================================================
            // UPDATE / CREATE
            // =========================================================

            foreach (var request in requests)
            {
                // Existing
                if (request.Id.HasValue)
                {
                    var image =
                        existingImages.FirstOrDefault(
                            x => x.Id == request.Id.Value);

                    if (image == null)
                    {
                        throw new InvalidOperationException(
                            $"Variant image {request.Id} không hợp lệ.");
                    }

                    if (request.File != null)
                    {
                        await ReplaceVariantImageAsync(
                            image,
                            request.File,
                            cancellationToken);
                    }

                    continue;
                }

                // New
                if (request.File == null)
                {
                    continue;
                }

                await AddNewVariantImageAsync(
                    product,
                    request,
                    cancellationToken);
            }
        }
        private async Task<List<ProductVariantMedia>>
  GetExistingVariantImagesAsync(
      ProductEntity product,
      CancellationToken cancellationToken)
        {
            return await _dbContext.ProductVariantMedias
                .Include(x => x.MediaFile)
                .Include(x => x.AttributeValue)
                .Where(x =>
                    x.ProductVariant.ProductId == product.Id)
                .ToListAsync(cancellationToken);
        }
        private async Task DeleteVariantImageAsync(
    ProductVariantMedia image,
    CancellationToken cancellationToken)
        {
            if (image == null)
            {
                return;
            }

            // =========================================================
            // GET MEDIA FILE
            // =========================================================

            var mediaFile = image.MediaFile;

            // =========================================================
            // REMOVE PRODUCT VARIANT MEDIA
            // =========================================================

            _dbContext.ProductVariantMedias.Remove(image);

            // =========================================================
            // REMOVE MEDIA FILE
            // =========================================================

            if (mediaFile != null)
            {
                _dbContext.MediaFiles.Remove(mediaFile);

                await _mediaService.DeleteAsync(
                    mediaFile,
                    cancellationToken);
            }
        }
        private async Task AddNewVariantImageAsync(
    ProductEntity product,
    CreateProductVariantImageRequest request,
    CancellationToken cancellationToken)
        {
            if (request.File == null)
            {
                return;
            }

            // =========================================================
            // PARSE KEY
            // =========================================================

            var parts = request.Key.Split(
                ':',
                2,
                StringSplitOptions.TrimEntries);

            if (parts.Length != 2 ||
                string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1]))
            {
                throw new InvalidOperationException(
                    $"Variant image key '{request.Key}' không hợp lệ.");
            }

            var attributeName = parts[0];
            var valueName = parts[1];

            // =========================================================
            // FIND ATTRIBUTE
            // =========================================================

            var attribute = await _dbContext.Attributes
                .FirstOrDefaultAsync(
                    x =>
                        x.Code == attributeName ||
                        x.Name == attributeName,
                    cancellationToken);

            if (attribute == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy Attribute '{attributeName}'.");
            }

            // =========================================================
            // FIND ATTRIBUTE VALUE
            // =========================================================

            var attributeValue =
                await _dbContext.AttributeValues
                    .FirstOrDefaultAsync(
                        x =>
                            x.AttributeId == attribute.Id &&
                            x.Value == valueName,
                        cancellationToken);

            if (attributeValue == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy AttributeValue '{valueName}'.");
            }

            // =========================================================
            // FIND ALL VARIANTS
            // =========================================================

            var variants =
                product.Variants
                    .Where(x =>
                        x.VariantAttributes.Any(
                            va =>
                                va.AttributeValueId ==
                                attributeValue.Id))
                    .ToList();

            if (variants.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy variant chứa '{request.Key}'.");
            }

            // =========================================================
            // SAVE MEDIA ONCE
            // =========================================================

            var mediaFile =
                await SaveMediaFileAsync(
                    request.File,
                    cancellationToken);

            // =========================================================
            // CREATE MAPPING FOR ALL VARIANTS
            // =========================================================

            foreach (var variant in variants)
            {
                var variantMedia = new ProductVariantMedia
                {
                    ProductVariantId = variant.Id,

                    AttributeValueId =
                        attributeValue.Id,

                    MediaFileId =
                        mediaFile.Id,

                    DisplayOrder = 0,

                    ProductVariant = variant,

                    AttributeValue = attributeValue,

                    MediaFile = mediaFile
                };

                variant.VariantMedias.Add(
                    variantMedia);
            }
        }
        private async Task ReplaceVariantImageAsync(
    ProductVariantMedia image,
    IFormFile file,
    CancellationToken cancellationToken)
        {
            // =========================================================
            // OLD MEDIA
            // =========================================================

            var oldMediaFile =
                image.MediaFile;

            // =========================================================
            // SAVE NEW MEDIA
            // =========================================================

            var newMediaFile =
                await SaveMediaFileAsync(
                    file,
                    cancellationToken);

            // =========================================================
            // UPDATE RELATION
            // =========================================================

            image.MediaFileId =
                newMediaFile.Id;

            image.MediaFile =
                newMediaFile;

            // =========================================================
            // DELETE OLD MEDIA
            // =========================================================

            if (oldMediaFile != null)
            {
                DeleteMediaFile( oldMediaFile);
            }
        }
        private async Task<ProductVariant?>
    FindVariantByAttributeValueAsync(
        int productId,
        int attributeValueId,
        CancellationToken cancellationToken)
        {
            return await _dbContext.ProductVariants
                .Include(x => x.VariantAttributes)
                .FirstOrDefaultAsync(
                    x =>
                        x.ProductId == productId &&
                        x.VariantAttributes.Any(
                            va =>
                                va.AttributeValueId ==
                                attributeValueId),
                    cancellationToken);
        }
       
        private void DeleteMediaFile(
    MediaFile? mediaFile)
        {
            if (mediaFile == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(
                    mediaFile.StoragePath))
            {
                var physicalPath =
                    Path.Combine(
                        _mediaStorage.RootPath,
                        mediaFile.StoragePath
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar));

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }

            _dbContext.MediaFiles.Remove(
                mediaFile);
        }
       
        private async Task<ServiceResult<int>>
    ValidateUpdateProductAsync(
        ProductEntity product,
        CreateProductRequest request,
        CancellationToken cancellationToken)
        {
            var sku = request.SKU.Trim();

            var skuExists =
                await _dbContext.Products.AnyAsync(
                    x =>
                        x.Id != product.Id &&
                        x.Sku == sku,
                    cancellationToken);

            if (skuExists)
            {
                return ServiceResult<int>.Fail(
                    "SKU đã tồn tại.");
            }

            var slug = request.Slug.Trim();

            var slugExists =
                await _dbContext.Products.AnyAsync(
                    x =>
                        x.Id != product.Id &&
                        x.Slug == slug,
                    cancellationToken);

            if (slugExists)
            {
                return ServiceResult<int>.Fail(
                    "Slug đã tồn tại.");
            }

            return ServiceResult<int>.Success(
                product.Id);
        }
        #endregion Update product

        public async Task<ServiceResult> MoveToDraftAsync(DeleteFormRequest request, CancellationToken cancellationToken)
        {
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id && !x.IsDeleted,
                    cancellationToken);

            if (product == null)
            {
                return ServiceResult.Fail(
                    "Product",
                    "Không tìm thấy sản phẩm.");
            }

            product.Status = ProductStatus.Draft;
            product.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<ProductStatus>> ToggleSuspendAsync(int productId, CancellationToken cancellationToken)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);

            if (product == null)
            {
                return ServiceResult<ProductStatus>.Fail("Product not found.");
            }

            try
            {
                if (product.Status == ProductStatus.Active)
                {
                    product.Status = ProductStatus.Draft;
                    product.UpdatedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return new ServiceResult<ProductStatus>
                    {
                        Succeeded = true,
                        Data = ProductStatus.Draft,
                        Message = "Product suspended successfully."
                    };
                }

                if (product.Status == ProductStatus.Draft)
                {
                    product.Status = ProductStatus.Active;
                    product.UpdatedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return new ServiceResult<ProductStatus>
                    {
                        Succeeded = true,
                        Data = ProductStatus.Active,
                        Message = "Product unsuspended successfully."
                    };
                }

                return new ServiceResult<ProductStatus>
                {
                    Succeeded = false,
                    Data = product.Status,
                    Message = "Product cannot be suspended or unsuspended."
                };
            }
            catch (DbUpdateException)
            {
                return new ServiceResult<ProductStatus>
                {
                    Succeeded = false,
                    Data = product.Status,
                    Message = "Unable to update product status."
                };
            }
            catch (Exception)
            {
                return new ServiceResult<ProductStatus>
                {
                    Succeeded = false,
                    Data = product.Status,
                    Message = "An unexpected error occurred while updating the product."
                };
            }
        }
        private async Task UpdateImagesAsync(
   ProductEntity product,
    List<CreateProductImageRequest> requests,
    CancellationToken cancellationToken)
        {
            var existingMedias = product.ProductMedias.ToList();

            var requestMediaIds = requests
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            // ==========================================
            // DELETE PRODUCT MEDIA
            // ==========================================

            var mediasToDelete = existingMedias
                .Where(x => !requestMediaIds.Contains(x.Id))
                .ToList();

            if (mediasToDelete.Count > 0)
            {
                _dbContext.ProductMedias.RemoveRange(mediasToDelete);
            }

            // ==========================================
            // UPDATE / ADD
            // ==========================================

            foreach (var request in requests)
            {
                ProductMedia? media = null;

                if (request.Id.HasValue)
                {
                    media = existingMedias
                        .FirstOrDefault(x => x.Id == request.Id.Value);
                }

                // ==========================================
                // EXISTING MEDIA
                // ==========================================

                if (media != null)
                {
                    media.DisplayOrder = request.DisplayOrder;
                    media.IsPrimary = request.IsPrimary;

                    // Có file mới → tạo MediaFile mới
                    if (request.File != null)
                    {
                        var mediaFile = await UploadMediaAsync(
                            request.File,
                            cancellationToken);

                        media.MediaFileId = mediaFile.Id;
                    }

                    continue;
                }

                // ==========================================
                // NEW MEDIA
                // ==========================================

                if (request.File == null)
                {
                    continue;
                }

                var newMediaFile = await UploadMediaAsync(
                    request.File,
                    cancellationToken);

                var newMedia = new ProductMedia
                {
                    ProductId = product.Id,
                    MediaFileId = newMediaFile.Id,
                    DisplayOrder = request.DisplayOrder,
                    IsPrimary = request.IsPrimary
                };

                await _dbContext.ProductMedias.AddAsync(
                    newMedia,
                    cancellationToken);
            }
        }
        private async Task<MediaFile> UploadMediaAsync(
    IFormFile file,
    CancellationToken cancellationToken)
        {
            // TODO:
            // upload physical/cloud storage
            // lấy URL/path

            var mediaFile = new MediaFile
            {
                // tùy entity MediaFile thực tế của bạn
                // FileName = file.FileName,
                // ContentType = file.ContentType,
                // Size = file.Length,
                // Url = url
            };

            await _dbContext.MediaFiles.AddAsync(
                mediaFile,
                cancellationToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return mediaFile;
        }
            private async Task UpdateTagsAsync(
                ProductEntity product,
                string? tags,
                CancellationToken cancellationToken)
        {
            // =========================================================
            // PARSE TAGS
            // =========================================================

            var tagValues = string.IsNullOrWhiteSpace(tags)
                ? []
                : JsonSerializer.Deserialize<List<string>>(tags)
                    ?? [];

            var tagNames = tagValues
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // =========================================================
            // LẤY TAG ĐANG GẮN VỚI PRODUCT
            // =========================================================

            var oldMappings = await _dbContext.ProductTagMappings.Where(x => x.ProductId == product.Id).ToListAsync(cancellationToken);

            var oldTagIds = oldMappings
                .Select(x => x.TagId)
                .ToHashSet();

            // =========================================================
            // KHÔNG CÓ TAG MỚI
            // =========================================================

            if (tagNames.Count == 0)
            {
                if (oldMappings.Count > 0)
                {
                    _dbContext.ProductTagMappings.RemoveRange(
                        oldMappings);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                return;
            }

            // =========================================================
            // LẤY PRODUCT TAG ĐÃ TỒN TẠI
            // =========================================================

            var existingTags = await _dbContext.ProductTags
                .Where(x => tagNames.Contains(x.Name))
                .ToListAsync(cancellationToken);

            var tagsByName = existingTags.ToDictionary(
                x => x.Name,
                StringComparer.OrdinalIgnoreCase);

            // =========================================================
            // TẠO PRODUCT TAG MỚI NẾU CHƯA CÓ
            // =========================================================

            foreach (var tagName in tagNames)
            {
                if (tagsByName.ContainsKey(tagName))
                {
                    continue;
                }

                var tag = new ProductTag
                {
                    Name = tagName,
                    Slug = await GenerateUniqueSlugCategoryAsync(
                        tagName)
                };

                await _dbContext.ProductTags.AddAsync(
                    tag,
                    cancellationToken);

                tagsByName[tagName] = tag;
            }

            // =========================================================
            // SAVE TAG MỚI ĐỂ CÓ TagId
            // =========================================================

            var hasNewTags = tagsByName.Values
                .Any(x => x.Id == 0);

            if (hasNewTags)
            {
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            // =========================================================
            // TAG ID MỚI
            // =========================================================

            var newTagIds = tagsByName.Values
                .Select(x => x.Id)
                .ToHashSet();

            // =========================================================
            // XÓA MAPPING CŨ KHÔNG CÒN ĐƯỢC CHỌN
            // =========================================================

            var mappingsToRemove = oldMappings
                .Where(x => !newTagIds.Contains(x.TagId))
                .ToList();

            if (mappingsToRemove.Count > 0)
            {
                _dbContext.ProductTagMappings.RemoveRange(
                    mappingsToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            // ========================================================= // CẬP NHẬT DISPLAY ORDER CHO MAPPING ĐÃ TỒN TẠI // =========================================================
            var currentMappings = oldMappings .Where(x => newTagIds.Contains(x.TagId)) .ToList(); 
            foreach (var mapping in currentMappings) { 
                var tag = tagsByName.Values .FirstOrDefault(x => x.Id == mapping.TagId); 
                if (tag == null) { continue; } 
                var displayOrder = tagNames.FindIndex( x => string.Equals( x, tag.Name, StringComparison.OrdinalIgnoreCase)); 
                if (displayOrder >= 0) { mapping.DisplayOrder = displayOrder; } 
            }
            // =========================================================
            // THÊM MAPPING MỚI CHƯA TỒN TẠI
            // =========================================================

            var mappingsToAdd = tagNames
                .Select((tagName, index) =>
                {
                    var tag = tagsByName[tagName];

                    return new
                    {
                        TagId = tag.Id,
                        DisplayOrder = index
                    };
                })
                .Where(x => !oldTagIds.Contains(x.TagId))
                .Select(x => new ProductTagMapping
                {
                    ProductId = product.Id,
                    TagId = x.TagId,
                    DisplayOrder = x.DisplayOrder
                })
                .ToList();

            if (mappingsToAdd.Count > 0)
            {
                await _dbContext.ProductTagMappings.AddRangeAsync(
                    mappingsToAdd,
                    cancellationToken);
            }
        }

        public async Task<bool> ExistsBySKUProductAsync(string sku)
        {
            try
            {
                await _dbContext.Products.AnyAsync(x => x.Sku == sku && !x.IsDeleted);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
            return await _dbContext.Products.AnyAsync(x => x.Sku == sku && !x.IsDeleted);
        }
        public async Task<bool> ExistsBySlugProductAsync(string slug)
        {
            return await _dbContext.Products.AnyAsync(x => x.Slug == slug && !x.IsDeleted);
        }
        public async Task<string> GenerateUniqueSlugProductAsync(string name)
        {
            var baseSlug = SlugHelper.Generate(name);

            var slug = baseSlug;
            var index = 2;

            while (await _dbContext.ProductCategories.AnyAsync(x => !x.IsActive && x.Slug == slug))
            {
                slug = $"{baseSlug}-{index}";
                index++;
            }

            return slug;
        }
        #region Caterogy 

        public async Task<PagedResult<ProductCategoryListResult>> GetCategoryListAsync(DataTableRequest request)
        {
            var query = _dbContext.ProductCategories.AsNoTracking().Where(x => !x.IsDeleted);

            // Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x =>
                    EF.Functions.ILike(x.Name, $"%{search}%"));
            }

            var filteredCount = await query.CountAsync();

            query = query.Where(x => !x.IsDeleted).OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ThenBy(x => x.Name);

            var totalCount = await _dbContext.ProductCategories.CountAsync();

            var items = await query
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new ProductCategoryListResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Slug = x.Slug,
                    IsActive = x.IsActive,
                    ProductCount = x.Products.Count(p => !p.IsDeleted)
                })
                .ToListAsync();

            return new PagedResult<ProductCategoryListResult>
            {
                Items = items,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }
        public async Task<EditProductCategoryRequest> GetCategoryIdAsync(int categoryId)
        {
            var category = _dbContext.ProductCategories.SingleOrDefault(x => x.Id == categoryId);
            var result = new EditProductCategoryRequest
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                SeoTitle = category.SeoTitle,
                SeoDescription = category.SeoDescription,
                SeoKeywords = category.SeoKeywords,
                IsActive = category.IsActive,
            };
            return result;
        }

        public async Task<bool> ExistsBySlugCategoryAsync(string slug)
        {
            return await _dbContext.ProductCategories.AnyAsync(x => x.Slug == slug && !x.IsDeleted);
        }
        public async Task<string> GenerateUniqueSlugCategoryAsync(string name)
        {
            var baseSlug = SlugHelper.Generate(name);

            var slug = baseSlug;
            var index = 2;

            while (await _dbContext.ProductCategories.AnyAsync(x => !x.IsActive && x.Slug == slug))
            {
                slug = $"{baseSlug}-{index}";
                index++;
            }

            return slug;
        }

        public async Task<ServiceResult<int>> SaveCategoryAsync(EditProductCategoryRequest model, CancellationToken cancellationToken = default)
        {
            if (model.Id == 0)
            {
                return await CreateProductCategoryAsync(model, cancellationToken);
            }

            return await UpdateProductCategoryAsync(model, cancellationToken);
        }
        private async Task<ServiceResult<int>> CreateProductCategoryAsync(EditProductCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var name = request.Name.Trim();
            var slug = request.Slug;

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<int>.Fail($"Name: không để trống.");

            if (string.IsNullOrWhiteSpace(slug))
                return ServiceResult<int>.Fail($"Slug: không để trống.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {

                var category = new ProductCategory
                {
                    Name = name,
                    Slug = slug,
                    Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),

                    SeoTitle = string.IsNullOrWhiteSpace(request.SeoTitle) ? null : request.SeoTitle.Trim(),

                    SeoDescription = string.IsNullOrWhiteSpace(request.SeoDescription) ? null : request.SeoDescription.Trim(),

                    SeoKeywords = string.IsNullOrWhiteSpace(request.SeoKeywords) ? null : request.SeoKeywords.Trim(),

                    //NoIndex = request.NoIndex,
                    //DisplayOrder = request.DisplayOrder,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ProductCategories.Add(category);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ServiceResult<int>.Success(category.Id);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                return ServiceResult<int>.Fail("Thêm danh mục thất bại");
            }
        }
        private async Task<ServiceResult<int>> UpdateProductCategoryAsync(EditProductCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var category = await _dbContext.ProductCategories.FirstOrDefaultAsync(x => x.Id == request.Id);

            if (category == null)
            {
                return ServiceResult<int>.Fail($"Name: không tồn tại.");
            }
            var nameExists = await _dbContext.ProductCategories.AnyAsync(x => x.Id != request.Id && x.Name == request.Name && !x.IsDeleted);

            if (nameExists)
                return ServiceResult<int>.Fail(
                 new ServiceError
                 {
                     Field = nameof(EditProductCategoryRequest.Slug),
                     Message = $"Slug: '{request.Name}' đã tồn tại."
                 });
            var normalizedSlug = request.Slug.Trim().ToLower();
            // Check duplicate slug
            var slugExists = await _dbContext.ProductCategories.AnyAsync(x => x.Id != request.Id && x.Slug == request.Slug && !x.IsDeleted && x.Slug.ToLower() == normalizedSlug);

            if (slugExists)
                return ServiceResult<int>.Fail(
                 new ServiceError
                 {
                     Field = nameof(EditProductCategoryRequest.Slug),
                     Message = $"Slug: '{request.Slug}' đã tồn tại."
                 });


            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {

                category.Name = request.Name;
                category.Slug = request.Slug;
                category.Description = request.Description;
                category.IsActive = request.IsActive;
                //category.ImageUrl = request.ImageUrl;
                category.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ServiceResult<int>.Success(category.Id);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                return ServiceResult<int>.Fail("Thêm danh mục thất bại");
            }
        }

        public async Task<ServiceResult<int>> DeleteAsync(DeleteFormRequest request)
        {
            var category = await _dbContext.ProductCategories
                .FirstOrDefaultAsync(x =>
                    x.Id == request.Id);

            if (category == null)
            {
                return ServiceResult<int>.Fail(
                    new ServiceError
                    {
                        Message = "Category không tồn tại hoặc đã được xóa."
                    });
            }

            // Nếu category đang được sử dụng bởi product
            var hasProducts = await _dbContext.Products.AnyAsync(x => x.CategoryId == request.Id && !x.IsDeleted);

            if (hasProducts)
            {
                return ServiceResult<int>.Fail(
                    new ServiceError
                    {
                        Message = "Không thể xóa category đang có sản phẩm."
                    });
            }

            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return ServiceResult<int>.Success(category.Id);
        }
        #endregion Category
    }
}
