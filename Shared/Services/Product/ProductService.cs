using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Shared.Constants.Core;
using Shared.Data.Context;
using Shared.Data.Entities.Catelog;
using Shared.Data.Entities.Inventory;
using Shared.Data.Entities.Media;
using Shared.Data.Entities.Product;
using Shared.DTOs.Identity;
using Shared.DTOs.Inventory;
using Shared.DTOs.Product;
using Shared.Enums;
using Shared.Interfaces.Core;
using Shared.Interfaces.Media;
using Shared.Requests;
using Shared.Requests.Product;
using Shared.Requests.Product.Category;
using Shared.Responses;
using Shared.Responses.Datatables;
using Shared.Responses.Product;
using Shared.Services.Order;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Shared.Common.CommonHelper;
using static System.Net.Mime.MediaTypeNames;
using ProductEntity = Shared.Data.Entities.Product.Product;

namespace Shared.Services.Product
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _dbContext;
        private readonly MediaStorageOptions _mediaStorage;
        private readonly IMediaService _mediaService;

        private readonly ILogger<ProductService> _logger;
        public ProductService(AppDbContext dbContext, IOptions<MediaStorageOptions> mediaStorage, IMediaService mediaService,
            ILogger<ProductService> logger)
        {
            _dbContext = dbContext;
            _mediaService = mediaService;
            _mediaStorage = mediaStorage.Value;

            _logger = logger;
        }

        public async Task<PagedResult<ProductListResult>> GetProductsAsync( DataTableResponse request)
        {
            var query = _dbContext.Products.AsNoTracking().AsQueryable();

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x => x.Name.Contains(search) || (x.Sku != null && x.Sku.Contains(search)));
            }

            if (request.Status.HasValue && Enum.IsDefined(typeof(ProductStatus), request.Status.Value))
            {
                var status = (ProductStatus)request.Status.Value;

                query = query.Where(x => x.Status == status);
            }

            if (request.Stock.HasValue)
            {
                if (request.Stock.Value == 1)
                {
                    // Còn hàng
                    query = query.Where(x => x.Variants.Where(v => v.IsActive).SelectMany(v => v.InventoryStocks)
                            .Any(s => s.AvailableQuantity > 0));
                }
                else if (request.Stock.Value == 0)
                {
                    // Hết hàng
                    query = query.Where(x => !x.Variants.Where(v => v.IsActive)
                            .SelectMany(v => v.InventoryStocks).Any(s => s.AvailableQuantity > 0));
                }
            }

            var filteredCount = await query.CountAsync();

            var sortColumn = request.SortColumn?.ToLower();

            query = sortColumn switch
            {
                "productname" or "name" => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),

                "sku" => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Sku) : query.OrderBy(x => x.Sku),

                "price" => request.SortDirection == "desc" ? query.OrderByDescending(x =>
                            x.Variants.Where(v => v.IsActive).Select(v => (decimal?)v.Price).Min() ?? x.Price)
                        : query.OrderBy(x => x.Variants.Where(v => v.IsActive).Select(v => (decimal?)v.Price).Min() ?? x.Price),

                "status" => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),

                _ => query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            };

            var items = await query.Skip(request.Start).Take(request.Length).Select(x => new ProductListResult
                {
                    Id = x.Id,

                    Name = x.Name,

                    Slug = x.Slug,

                    ShortDescription = x.ShortDescription,

                    ImageUrl = x.ProductMedias.Where(i => i.IsPrimary)
                        .Select(i => i.MediaFile.StoragePath == null ? null
                                : i.MediaFile.StoragePath.StartsWith("/uploads/") ? i.MediaFile.StoragePath : "/uploads/" + i.MediaFile.StoragePath)
                        .FirstOrDefault(),

                    CategoryId = x.CategoryId,

                    CategoryName = x.Category != null ? x.Category.Name : string.Empty,

                    Sku = x.Sku,

                    MinPrice = x.Variants.Where(v => v.IsActive).Select(v => (decimal?)v.Price).Min() ?? x.Price,

                    MaxPrice = x.Variants.Where(v => v.IsActive).Select(v => (decimal?)v.Price).Max() ?? x.Price,

                    Quantity = x.Variants.Where(v => v.IsActive).SelectMany(v => v.InventoryStocks).Sum(s => s.AvailableQuantity),

                    InStock = x.Variants.Where(v => v.IsActive).SelectMany(v => v.InventoryStocks).Any(s => s.AvailableQuantity > 0),

                    Status = x.Status,

                    IsFeatured = x.IsFeatured,

                    DisplayOrder = x.DisplayOrder,

                    CreatedAt = x.CreatedAt
                }).ToListAsync();

            //Result
            return new PagedResult<ProductListResult>
            {
                Items = items,

                Page = request.Length > 0 ? request.Start / request.Length + 1 : 1,

                PageSize = request.Length,

                TotalCount = totalCount,

                FilteredCount = filteredCount
            };
        }
        public async Task<List<SelectListItem>> GetCreateProductCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.ProductCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                }).ToListAsync(cancellationToken);
        }
        public async Task<List<SelectListItem>> GetCreateAttributesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Attributes.AsNoTracking().OrderBy(x => x.CreatedAt)
                .Select(x => new SelectListItem
                {
                    Value = x.Code.ToString(),
                    Text = x.Name
                }).ToListAsync(cancellationToken);
        }

        #region Create product 
        public async Task<ServiceResult<int>> CreateAsync(ProductRequest request, CancellationToken cancellationToken = default)
        {
            var sku = request.SKU.Trim();
            //validate SKU
            var skuExists = await _dbContext.Products.AnyAsync(x => x.Sku == sku, cancellationToken);

            if (skuExists)
            {
                return ServiceResult<int>.Fail($"SKU '{sku}' đã tồn tại.");
            }

            //Validate SLUG
            var slug = string.IsNullOrWhiteSpace(request.Slug) ? SlugHelper.Generate(request.Name) : SlugHelper.Generate(request.Slug);

            var slugExists = await _dbContext.Products.AnyAsync(x => x.Slug == slug, cancellationToken);

            if (slugExists)
            {
                slug = $"{slug}-{Guid.NewGuid():N}";
            }
            //Parse variant product
            var productVariants = string.IsNullOrWhiteSpace(request.Variants) ? [] :
                JsonSerializer.Deserialize<List<ProductVariantRequest>>(request.Variants,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })  ?? [];

            var status = request.Action switch
            {
                "publish" => ProductStatus.Active,
                "draft" => ProductStatus.Draft,
                _ => ProductStatus.Draft
            };
            
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
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

                    IsFeatured = request.IsFeatured,

                    Status = status,

                    CreatedAt = DateTime.UtcNow,
                };


                await _dbContext.Products.AddAsync(product, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                //Create product images

                if (request.ProductImages != null && request.ProductImages.Count > 0)
                {
                    await CreateProductImagesAsync(product, request.ProductImages, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                
                //Create tags product
                await CreateProductTagsAsync(product, request.Tags, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                //Create variants product
                if (productVariants != null && productVariants.Count > 0)
                {
                    var variants = await CreateVariantsAsync(product, productVariants, cancellationToken);
                    //Create variant attribute 
                    await CreateVariantAttributesAsync(variants, productVariants, cancellationToken);
                    //Create variant image
                    await CreateVariantImagesAsync(variants, request.VariantImages, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    var inventoryItems = variants.Select(variant =>
                    {
                        var variantRequest = productVariants.First(x => x.SKU == variant.Sku);

                        return new InitialInventoryItem
                        {
                            ProductVariantId = variant.Id,
                            Quantity = variantRequest.Stock
                        };
                    });
                    await CreateInventoryForVariantsAsync(inventoryItems, product.Id, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);

                }
                //Create variant product ảo
                else
                {
                    var defaultVariant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Sku = product.Sku,
                        Price = request.Price ?? 0,
                        IsDefault = true,
                        IsActive = true,
                        DisplayOrder = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.ProductVariants.AddAsync(defaultVariant, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    //Create stock
                    await CreateInventoryForVariantsAsync(new[]
                    {
                        new InitialInventoryItem
                        {
                            ProductVariantId = defaultVariant.Id,
                            Quantity = request.Stock ?? 0
                        }
                    }, product.Id, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return ServiceResult<int>.Success(product.Id);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);

                return ServiceResult<int>.Fail("Không thể lưu sản phẩm. Vui lòng thử lại.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);

                return ServiceResult<int>.Fail("Đã xảy ra lỗi khi tạo sản phẩm.");
            }
        }
        private async Task CreateProductImagesAsync(ProductEntity product, List<ProductImageRequest>? images, CancellationToken cancellationToken = default)
        {
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

        private async Task<List<ProductVariant>> CreateVariantsAsync(ProductEntity product, ICollection<ProductVariantRequest>? requests,
        CancellationToken cancellationToken = default)
        {
            if (requests == null || requests.Count == 0)
            {
                return [];
            }

            var requestList = requests.ToList();

            await ValidateVariantSkusAsync(product.Id, requests, cancellationToken);

            var variants = new List<ProductVariant>(requestList.Count);

            for (var i = 0; i < requestList.Count; i++)
            {
                var request = requestList[i];

                var variant = BuildVariant(product, request, i);

                product.Variants.Add(variant);

                variants.Add(variant);
            }

            return variants;
        }
        private async Task CreateVariantAttributesAsync(IEnumerable<ProductVariant> variants, ICollection<ProductVariantRequest> requests,
        CancellationToken cancellationToken = default)
        {
            var variantList = variants.ToList();
            var requestList = requests.ToList();

            if (variantList.Count == 0)
            {
                return;
            }

            if (variantList.Count != requestList.Count)
            {
                throw new InvalidOperationException("Số lượng variant không khớp với số lượng request.");
            }

            for (var i = 0; i < variantList.Count; i++)
            {
                await AddVariantAttributesAsync(variantList[i], requestList[i].Options, cancellationToken);
            }
        }
        private async Task CreateVariantImagesAsync(IEnumerable<ProductVariant> variants, IEnumerable<ProductVariantImageRequest>? variantImages,
        CancellationToken cancellationToken = default)
        {
            if (variantImages == null)
                return;

            var variantList = variants.ToList();

            if (variantList.Count == 0)
                return;

            foreach (var imageRequest in variantImages)
            {
                var key = imageRequest.Key?.Trim();
                var file = imageRequest.File;

                if (string.IsNullOrWhiteSpace(key) || file == null || file.Length == 0)
                    continue;

                var parts = key.Split(':', 2, StringSplitOptions.TrimEntries);

                if (parts.Length != 2)
                    continue;

                var attributeName = parts[0];
                var attributeValueName = parts[1];

                if (string.IsNullOrWhiteSpace(attributeName) || string.IsNullOrWhiteSpace(attributeValueName))
                    continue;

                var matchingVariants = variantList .Where(variant =>
                        variant.VariantAttributes.Any(va => string.Equals(va.AttributeValue.Attribute.Code,
                                attributeName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(va.AttributeValue.Value, attributeValueName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (matchingVariants.Count == 0)
                    continue;

                var attributeValue = matchingVariants.SelectMany(x => x.VariantAttributes).Select(x => x.AttributeValue)
                    .FirstOrDefault(x => string.Equals( x.Attribute.Code, attributeName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals( x.Value, attributeValueName, StringComparison.OrdinalIgnoreCase));

                if (attributeValue == null)
                    continue;

                var mediaFile = await SaveMediaFileAsync(file, cancellationToken);

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
        {
            if (string.IsNullOrWhiteSpace(tags))
                return;

            var tagValues = JsonSerializer.Deserialize<List<string>>(tags) ?? [];
            var tagNames = tagValues.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (tagNames.Count == 0)
                return;

            var tagRequests = tagNames.Select(name =>  new 
                { 
                    Name = name, 
                    Slug = SlugHelper.Generate(name) 
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Slug)).GroupBy(x => x.Slug, StringComparer.OrdinalIgnoreCase).
                Select(x => x.First()).ToList();
            
            if (tagRequests.Count == 0)
                return;

            var slugs = tagRequests.Select(x => x.Slug).ToList();

            var existingTags = await _dbContext.ProductTags.Where(x => slugs.Contains(x.Slug)).ToListAsync(cancellationToken);
            var tagBySlug = existingTags.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);
            var newTags = new List<ProductTag>();
            foreach (var request in tagRequests)
            {
                if (tagBySlug.ContainsKey(request.Slug))
                    continue;

                var tag = new ProductTag 
                { 
                    Name = request.Name, 
                    Slug = request.Slug, 
                    IsActive = true, NoIndex = true, 
                    CreatedAt = DateTime.UtcNow 
                };

                newTags.Add(tag);
                tagBySlug[request.Slug] = tag;
            }
            if (newTags.Count > 0)
                await _dbContext.ProductTags.AddRangeAsync(newTags, cancellationToken);

            var mappings = tagRequests.Select(request => tagBySlug[request.Slug]).
                Select(tag => new ProductTagMapping { ProductId = product.Id, Tag = tag }).ToList();

            if (mappings.Count == 0)
                return;

            await _dbContext.ProductTagMappings.AddRangeAsync(mappings, cancellationToken);
        }
        
        private async Task CreateInventoryForVariantsAsync(IEnumerable<InitialInventoryItem> items, int referenceId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            foreach (var item in items)
            {
                var stock = new InventoryStock
                {
                    WarehouseId = WarehouseConstants.MainWarehouseId,
                    ProductVariantId = item.ProductVariantId,
                    AvailableQuantity = item.Quantity,
                    ReservedQuantity = 0,
                    MinStock = 0,
                    UpdatedAt = now
                };

                var transaction = new InventoryTransaction
                {
                    WarehouseId = WarehouseConstants.MainWarehouseId,
                    ProductVariantId = item.ProductVariantId,
                    Type = InventoryTransactionType.Import,
                    Quantity = item.Quantity,
                    ReferenceId = referenceId,
                    Note = "",
                    CreatedAt = now
                };

                await _dbContext.InventoryStocks.AddAsync(stock, cancellationToken);
                await _dbContext.InventoryTransactions.AddAsync(transaction, cancellationToken);
            }
        }
        private static string BuildVariantName(Dictionary<string, string> options)
        {
            return string.Join(" - ", options.Values);
        }
        #endregion Create product 

        #region Update product
        public async Task<ServiceResult<int>> UpdateAsync(ProductRequest request, CancellationToken cancellationToken = default)
        {
            var warehouseId = WarehouseConstants.MainWarehouseId;
            
            if (request.Id <= 0)
            {
                return ServiceResult<int>.Fail("Sản phẩm không hợp lệ.");
            }

            var product = await _dbContext.Products
                .Include(x => x.ProductMedias).ThenInclude(x => x.MediaFile)
                .Include(x => x.Variants).ThenInclude(x => x.VariantAttributes).ThenInclude(x => x.AttributeValue).ThenInclude(x => x.Attribute)
                .Include(x => x.Variants).ThenInclude(x => x.InventoryStocks).Include(x => x.Variants).ThenInclude(x => x.InventoryTransactions)
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id,
                    cancellationToken);

            if (product == null)
            {
                return ServiceResult<int>.Fail("Không tìm thấy sản phẩm.");
            }

            var validationResult = await ValidateUpdateProductAsync(product, request, cancellationToken);

            if (!validationResult.Succeeded)
            {
                return validationResult;
            }
           
            var options = DeserializeOptions(request.Options);

            if (options == null)
            {
                return ServiceResult<int>.Fail("Dữ liệu option không hợp lệ.");
            }

            var variants = DeserializeVariants(request.Variants);

            if (variants == null)
            {
                return ServiceResult<int>.Fail("Dữ liệu variant không hợp lệ.");
            }
            //Xóa file vật lý nên cần lưu file để xóa sau khi transaction commit thành công
            var filesToDelete = new List<MediaFile>();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                UpdateBasicProduct(product, request);

                await UpdateProductImagesAsync(product, request.ProductImages, cancellationToken);

                await UpdateTagsAsync(product, request.Tags, cancellationToken);

                await UpdateVariantsAsync(product, options, variants, cancellationToken);

                await UpdateInventoryAsync(product, request.Stock, variants, cancellationToken);
               
                await UpdateVariantImagesAsync(product, request.VariantImages, filesToDelete, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                
                foreach (var mediaFile in filesToDelete)
                {
                    try
                    {
                        await _mediaService.DeleteAsync(mediaFile, cancellationToken);
                    }
                    catch
                    {
                        return ServiceResult<int>.Fail("Cập nhật thành công!. Hình ảnh sản phẩm cũ xóa thất bại");
                    }
                }
                return ServiceResult<int>.Success(product.Id, "Cập nhật sản phẩm thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                return ServiceResult<int>.Fail(ex.Message);
            }
        }
        public async Task<ServiceResult<ProductResponse>> GetUpdateProductAsync(int id, CancellationToken cancellationToken = default)
        {
            // =========================================================
            // LOAD PRODUCT
            // =========================================================
            var product = new ProductEntity();
            try
            {
                product = await _dbContext.Products
                    .AsNoTracking()
                    .Include(x => x.ProductMedias).ThenInclude(x => x.MediaFile)
                    .Include(x => x.ProductTagMappings).ThenInclude(x => x.Tag)
                    .Include(x => x.Variants.Where(v => v.IsActive)).ThenInclude(x => x.VariantAttributes).ThenInclude(x => x.AttributeValue).ThenInclude(x => x.Attribute)
                    .Include(x => x.Variants.Where(v => v.IsActive)).ThenInclude(x => x.VariantMedias).ThenInclude(x => x.MediaFile)
                    .Include(x => x.Variants.Where(v => v.IsActive)).ThenInclude(x => x.VariantMedias).ThenInclude(x => x.AttributeValue).ThenInclude(x => x.Attribute)
                    .Include(x => x.Variants.Where(v => v.IsActive)).ThenInclude(x => x.InventoryStocks)
                    .FirstOrDefaultAsync(
                        x => x.Id == id,
                        cancellationToken);

            }
            catch (Exception ex)
            {
                return ServiceResult<ProductResponse>.Fail(
                   ex.Message.ToString());

            }

            if (product == null)
            {
                return ServiceResult<ProductResponse>.Fail(
                    $"Không tìm thấy sản phẩm có Id = {id}.");
            }

            // =========================================================
            // RESPONSE
            // =========================================================

            var response = new ProductResponse
            {
                Id = product.Id,

                SKU = product.Sku,

                Name = product.Name,

                Slug = product.Slug,

                CategoryId = product.CategoryId,

                Tags = product.ProductTagMappings.Where(x => x.Tag != null).Select(x => x.Tag.Name).ToList(),
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

                    Url = BuildMediaUrl(x.MediaFile),

                    DisplayOrder =
                        x.DisplayOrder,

                    IsPrimary =
                        x.IsPrimary
                })
                .ToList();

            // =========================================================
            // PRODUCT STOCK
            // =========================================================

            var defaultVariant = product.Variants.FirstOrDefault(x => x.IsDefault);
            response.Stock = defaultVariant?.InventoryStocks
                .FirstOrDefault(x => x.WarehouseId == WarehouseConstants.MainWarehouseId)?.AvailableQuantity ?? 0;

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
                .Select(group => new ProductOptionResponse
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

                        Stock = variant.InventoryStocks.FirstOrDefault(x => x.WarehouseId == WarehouseConstants.MainWarehouseId)?.AvailableQuantity ?? 0,

                        IsDefault = variant.IsDefault,

                        IsActive = variant.IsActive,

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

            return ServiceResult<ProductResponse>.Success(
                response);
        }
        private string BuildMediaUrl(MediaFile mediaFile)
        {
            return "/" + mediaFile.StoragePath
                .Replace("\\", "/");
        }
        private static List<ProductOptionRequest> DeserializeOptions(string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<
                    List<ProductOptionRequest>
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
        private static List<ProductVariantRequest> DeserializeVariants(string? variantsJson)
        {
            if (string.IsNullOrWhiteSpace(variantsJson))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<
                    List<ProductVariantRequest>
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
        private static void UpdateBasicProduct(ProductEntity product, ProductRequest request)
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
        private async Task UpdateProductImagesAsync(ProductEntity product, List<ProductImageRequest> requests, CancellationToken cancellationToken)
        {
            // =========================================================
            // EXISTING IMAGES
            // =========================================================

            var existingImages = product.ProductMedias.ToList();

            // =========================================================
            // VALIDATE PRIMARY
            // =========================================================

            var primaryCount = requests.Count(x => x.IsPrimary);

            if (primaryCount > 1)
            {
                throw new InvalidOperationException(
                    "Sản phẩm chỉ được có một ảnh chính.");
            }

            // =========================================================
            // REQUEST IDS
            // =========================================================

            var requestIds = requests
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            // =========================================================
            // DELETE REMOVED IMAGES
            // =========================================================

            foreach (var existingImage in existingImages)
            {
                if (requestIds.Contains(existingImage.Id))
                {
                    continue;
                }

                var mediaFile = existingImage.MediaFile;

                product.ProductMedias.Remove(existingImage);

                _dbContext.ProductMedias.Remove(existingImage);

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
                // EXISTING IMAGE
                // =====================================================

                if (request.Id.HasValue)
                {
                    var existingImage = existingImages
                        .FirstOrDefault(x => x.Id == request.Id.Value);

                    if (existingImage == null)
                    {
                        throw new InvalidOperationException(
                            $"ProductMedia {request.Id.Value} không thuộc sản phẩm.");
                    }

                    existingImage.DisplayOrder = request.DisplayOrder;
                    existingImage.IsPrimary = request.IsPrimary;

                    // -------------------------------------------------
                    // REPLACE FILE
                    // -------------------------------------------------

                    if (request.File != null)
                    {
                        var oldMedia = existingImage.MediaFile;

                        var newMedia = await _mediaService.UploadAsync(
                            request.File,
                            _mediaStorage.ProductFolder,
                            cancellationToken);

                        existingImage.MediaFile = newMedia;
                        existingImage.MediaFileId = newMedia.Id;

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

                var media = await _mediaService.UploadAsync(
                    request.File,
                    _mediaStorage.ProductFolder,
                    cancellationToken);

                var productMedia = new ProductMedia
                {
                    ProductId = product.Id,
                    MediaFileId = media.Id,
                    MediaFile = media,
                    DisplayOrder = request.DisplayOrder,
                    IsPrimary = request.IsPrimary
                };

                product.ProductMedias.Add(productMedia);
            }

            // =========================================================
            // ENSURE ONLY ONE PRIMARY IMAGE
            // =========================================================

            var remainingImages = product.ProductMedias.ToList();

            var primaryImages = remainingImages
                .Where(x => x.IsPrimary)
                .ToList();

            if (primaryImages.Count > 1)
            {
                throw new InvalidOperationException(
                    "Sản phẩm chỉ được có một ảnh chính.");
            }

            // =========================================================
            // FALLBACK PRIMARY
            // =========================================================

            if (remainingImages.Count > 0 &&
                primaryImages.Count == 0)
            {
                var firstImage = remainingImages
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Id)
                    .First();

                firstImage.IsPrimary = true;
            }
        }
        private async Task UpdateVariantsAsync(ProductEntity product, List<ProductOptionRequest> options, List<ProductVariantRequest> requests, CancellationToken cancellationToken)
        {
            // =========================================================
            // VALIDATE
            // =========================================================

            ValidateVariantRequests(requests);
            await ValidateVariantSkusAsync(product.Id, requests, cancellationToken);

            var defaultVariant = product.Variants.FirstOrDefault(x => x.IsDefault);

            var existingVariants = product.Variants
                .Where(x => !x.IsDefault && x.IsActive)
                .ToList();
            var hasRealVariants = requests.Count > 0;
            // =========================================================
            // NO VARIANT
            // =========================================================

            if (!hasRealVariants)
            {
                if (defaultVariant == null)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy variant mặc định của sản phẩm.");
                }

                defaultVariant.IsActive = true;
                defaultVariant.Price = product.Price ?? 0;

                // -----------------------------------------
                // Deactivate all real variants
                // -----------------------------------------

                foreach (var variant in existingVariants)
                {
                    variant.IsActive = false;
                    variant.IsDefault = false;
                }

                return;
            }
            // =========================================================
            // PRODUCT HAS REAL VARIANTS
            // =========================================================

            // Default technical variant is no longer used
            if (defaultVariant != null)
            {
                defaultVariant.IsActive = false;
                defaultVariant.IsDefault = true;
            }


            // =========================================================
            // EXISTING REQUEST VARIANT IDS
            // =========================================================

            var requestIds = requests.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();


            // =========================================================
            // DETERMINE MAIN OPTION
            // =========================================================

            var mainOption = options.FirstOrDefault();

            if (mainOption == null ||
                string.IsNullOrWhiteSpace(mainOption.Name))
            {
                throw new InvalidOperationException("Không xác định được option chính của variant.");
            }

            var mainOptionName = mainOption.Name.Trim();


            // =========================================================
            // DETERMINE REQUESTED MAIN VALUES
            // =========================================================

            var requestedMainValues = requests.Select(x => GetOptionValue(x.Options, mainOptionName))
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);


            // =========================================================
            // UPDATE / CREATE REQUESTED VARIANTS
            // =========================================================
            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];

                if (request.Id.HasValue)
                {
                    await UpdateExistingVariantAsync(product, request, i, cancellationToken);

                    continue;
                }

                await CreateNewVariantAsync(product, request, i, cancellationToken);
            }


            // =========================================================
            // DEACTIVATE VARIANTS NO LONGER EXIST
            // =========================================================

            foreach (var variant in existingVariants)
            {
                // Variant vẫn tồn tại trong request
                if (requestIds.Contains(variant.Id))
                {
                    continue;
                }

                var mainValue = GetMainVariantValue(variant, mainOptionName);

                if (string.IsNullOrWhiteSpace(mainValue))
                {
                    variant.IsActive = false;
                    variant.IsDefault = false;

                    continue;
                }

                if (!requestedMainValues.Contains(mainValue))
                {
                    await DeactivateVariantGroupAsync(product, mainOptionName, mainValue, cancellationToken);
                }
                else
                {
                    variant.IsActive = false;
                    variant.IsDefault = false;
                }
            }
        }

        private async Task UpdateInventoryAsync(ProductEntity product, int? productStock, List<ProductVariantRequest> variants, CancellationToken cancellationToken)
        {
            var warehouseId = WarehouseConstants.MainWarehouseId;

            if (variants.Count == 0)
            {
                var defaultVariant = product.Variants.FirstOrDefault(x => x.IsActive && x.IsDefault);
                if (defaultVariant == null)
                {
                    throw new InvalidOperationException($"Product {product.Id} không có default variant.");
                }

                var stock = defaultVariant?.InventoryStocks.FirstOrDefault(x => x.WarehouseId == warehouseId);

                var oldStock = stock?.AvailableQuantity;
                var quantity = productStock ?? 0;
                var delta = quantity - (oldStock ?? 0);

                if (stock == null)
                {
                    stock = new InventoryStock
                    {
                        WarehouseId = warehouseId,
                        ProductVariantId = defaultVariant.Id,
                        AvailableQuantity = quantity,
                        ReservedQuantity = 0,
                        MinStock = 0,
                        CreateAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };

                    await _dbContext.InventoryStocks.AddAsync(stock, cancellationToken);
                }
                else
                {
                    stock.AvailableQuantity = quantity;
                    stock.UpdatedAt = DateTime.UtcNow;
                }
                if (delta != 0)
                {
                    //Stock thay đổi mới thêm transaction

                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction()
                    {
                        WarehouseId = warehouseId,
                        ProductVariantId = defaultVariant.Id,
                        Type = InventoryTransactionType.Adjustment,
                        ReferenceId = product.Id,
                        Note = "Cập nhật sản phẩm",
                        CreatedAt = DateTime.UtcNow

                    }, cancellationToken);
                }

                return;
            }

            var variantIds = product.Variants.Where(x => x.IsActive).Select(x => x.Id).ToList();

            var stocks = await _dbContext.InventoryStocks
                .Where(x => x.WarehouseId == warehouseId && variantIds.Contains(x.ProductVariantId))
                .ToListAsync(cancellationToken);

            foreach (var variantRequest in variants)
            {
                var variant = variantRequest.Id.HasValue ? product.Variants.FirstOrDefault(x => x.Id == variantRequest.Id.Value)
                    : product.Variants.FirstOrDefault(x => string.Equals(x.Sku, variantRequest.SKU.Trim(), StringComparison.OrdinalIgnoreCase));

                if (variant == null)
                {
                    continue;
                }

                var stock = stocks.FirstOrDefault(x => x.ProductVariantId == variant.Id);

                var oldStock = stock?.AvailableQuantity;
                var quantity = variantRequest.Stock;
                var delta = quantity - (oldStock ?? 0);

                if (stock == null)
                {
                    stock = new InventoryStock
                    {
                        WarehouseId = warehouseId,
                        AvailableQuantity = quantity,
                        ReservedQuantity = 0,
                        ProductVariant = variant,
                        MinStock = 0,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _dbContext.InventoryStocks.AddAsync(
                        stock,
                        cancellationToken);
                }
                else
                {
                    stock.AvailableQuantity = quantity;
                    stock.UpdatedAt = DateTime.UtcNow;
                }
                if (delta != 0)
                {
                    //Stock thay đổi mới thêm transaction
                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction()
                    {
                        WarehouseId = warehouseId,
                        ProductVariant = variant,
                        Type = InventoryTransactionType.Adjustment,
                        ReferenceId = product.Id,
                        Note = "Cập nhật sản phẩm",
                        CreatedAt = DateTime.UtcNow

                    }, cancellationToken);
                }
            }
        }
        private static string? GetOptionValue(Dictionary<string, string>? options, string optionName)
        {
            if (options == null || options.Count == 0 || string.IsNullOrWhiteSpace(optionName))
                return null;

            var option = options.FirstOrDefault(x =>
                string.Equals(x.Key?.Trim(), optionName.Trim(), StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(option.Key) ? null : option.Value?.Trim();
        }
        private static string? GetMainVariantValue(ProductVariant variant, string mainOptionName)
        {
            if (variant.VariantAttributes == null || variant.VariantAttributes.Count == 0 || string.IsNullOrWhiteSpace(mainOptionName))
                return null;

            mainOptionName = mainOptionName.Trim();

            var attributeValue = variant.VariantAttributes.Select(x => x.AttributeValue)
                .FirstOrDefault(x => x.Attribute != null &&
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
        private async Task DeactivateVariantGroupAsync(ProductEntity product, string mainOptionName, string mainValue,
        CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(mainOptionName) || string.IsNullOrWhiteSpace(mainValue))
                return;

            mainOptionName = mainOptionName.Trim();
            mainValue = mainValue.Trim();

            var attribute = await _dbContext.Attributes
                .FirstOrDefaultAsync(x => x.Code == mainOptionName || x.Name == mainOptionName, cancellationToken);

            if (attribute == null)
                throw new InvalidOperationException($"Không tìm thấy Attribute '{mainOptionName}'.");

            var attributeValue = await _dbContext.AttributeValues
                    .FirstOrDefaultAsync(x => x.AttributeId == attribute.Id && x.Value == mainValue, cancellationToken);

            if (attributeValue == null)
                throw new InvalidOperationException($"Không tìm thấy AttributeValue '{mainValue}'.");

            //FIND VARIANTS IN GROUP
            var groupVariants = product.Variants.Where(x =>
                    !x.IsDefault && x.VariantAttributes.Any(va => va.AttributeValueId == attributeValue.Id))
                .ToList();

            if (groupVariants.Count == 0)
                return;

            //DEACTIVATE
            foreach (var variant in groupVariants)
            {
                variant.IsActive = false;
                variant.IsDefault = false;
            }
        }
        private static void ValidateVariantRequests(List<ProductVariantRequest> requests)
        {
            if (requests == null || requests.Count == 0)
                return;

            //CHECK VALIDATE EACH VARIANT
            foreach (var request in requests)
            {
                if (request == null)
                    throw new InvalidOperationException("Dữ liệu variant không hợp lệ.");

                if (string.IsNullOrWhiteSpace(request.SKU))
                    throw new InvalidOperationException("SKU variant không được để trống.");

                if (request.Price < 0)
                    throw new InvalidOperationException($"Giá của variant '{request.SKU}' không hợp lệ.");

                if (request.Stock < 0)
                    throw new InvalidOperationException($"Tồn kho của variant '{request.SKU}' không hợp lệ.");

                if (request.Options == null ||  request.Options.Count == 0)
                    throw new InvalidOperationException($"Variant '{request.SKU}' phải có option.");

                foreach (var option in request.Options)
                {
                    if (string.IsNullOrWhiteSpace(option.Key))
                        throw new InvalidOperationException($"Variant '{request.SKU}' có tên option không hợp lệ.");

                    if (string.IsNullOrWhiteSpace(option.Value))
                        throw new InvalidOperationException($"Variant '{request.SKU}' có giá trị option không hợp lệ.");
                }
            }
            //CHECK DUPLICATE SKU
            var duplicateSku = requests.Where(x => !string.IsNullOrWhiteSpace(x.SKU)).GroupBy(x => x.SKU.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(x => x.Count() > 1);

            if (duplicateSku != null)
                throw new InvalidOperationException($"SKU variant '{duplicateSku.Key}' bị trùng.");

            var duplicateIds = requests.Where(x => x.Id.HasValue).GroupBy(x => x.Id!.Value).FirstOrDefault(x => x.Count() > 1);

            if (duplicateIds != null)
                throw new InvalidOperationException($"Variant ID '{duplicateIds.Key}' bị trùng.");
        }
        private async Task<ProductVariant> CreateNewVariantAsync(ProductEntity product, ProductVariantRequest request, int displayOrder,
        CancellationToken cancellationToken = default)
        {
            var sku = request.SKU?.Trim();

            if (string.IsNullOrWhiteSpace(sku))
                throw new InvalidOperationException("SKU variant không được để trống.");

            // Tìm variant cũ đã inactive
            var existingVariant = product.Variants
                .FirstOrDefault(x => !x.IsActive && !x.IsDefault && 
                string.Equals(x.Sku, sku, StringComparison.OrdinalIgnoreCase));

            if (existingVariant != null)
            {
                // Reactivate variant cũ
                existingVariant.IsActive = true;
                existingVariant.IsDefault = false;
                existingVariant.Name = BuildVariantName(request.Options);
                existingVariant.Price = request.Price;
                existingVariant.DisplayOrder = displayOrder;

                await UpdateVariantAttributesAsync(existingVariant, request.Options, cancellationToken);

                return existingVariant;
            }

            var variant = BuildVariant(product, request, displayOrder);

            product.Variants.Add(variant);

            await AddVariantAttributesAsync(variant, request.Options, cancellationToken);

            return variant;
        }
        private async Task UpdateVariantAttributesAsync(ProductVariant variant, Dictionary<string, string>? options, CancellationToken cancellationToken = default)
        {
            var existingAttributes = variant.VariantAttributes.ToList();

            if (existingAttributes.Count > 0)
                _dbContext.VariantAttributes.RemoveRange(existingAttributes);

            await AddVariantAttributesAsync(variant, options, cancellationToken);
        }
        private async Task UpdateExistingVariantAsync(ProductEntity product, ProductVariantRequest request, int displayOrder, CancellationToken cancellationToken = default)
        {
            if (!request.Id.HasValue)
                throw new InvalidOperationException("Variant ID không hợp lệ.");

            var variant = product.Variants.FirstOrDefault(x => x.Id == request.Id.Value);

            if (variant == null)
                throw new InvalidOperationException($"Variant ID {request.Id.Value} không thuộc sản phẩm.");

            variant.Name = BuildVariantName(request.Options);

            variant.Sku = request.SKU.Trim();

            variant.Price = request.Price;

            // Real variant
            variant.IsDefault = false;
            variant.IsActive = true;

            await UpdateVariantAttributesAsync(variant, request.Options, cancellationToken);
        }
        private async Task UpdateVariantImagesAsync(ProductEntity product, List<ProductVariantImageRequest> requests, List<MediaFile> filesToDelete,
        CancellationToken cancellationToken)
        {
            var existingImages = await GetExistingVariantImagesAsync(product, cancellationToken);

            var requestIds = requests.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();

            foreach (var image in existingImages)
            {
                if (requestIds.Contains(image.Id))
                    continue;

                await DeleteVariantImageAsync(image, filesToDelete, cancellationToken);
            }

            foreach (var request in requests)
            {
                if (request.Id.HasValue)
                {
                    var image = existingImages.FirstOrDefault(x => x.Id == request.Id.Value);

                    if (image == null)
                        throw new InvalidOperationException($"Variant image {request.Id} không hợp lệ.");

                    if (request.File != null)
                        await ReplaceVariantImageAsync(image, request.File, filesToDelete, cancellationToken);

                    continue;
                }

                // New VARIANT IMAGE
                if (request.File == null)
                    continue;

                await AddNewVariantImageAsync(product, request, cancellationToken);
            }
        }
        private async Task<List<ProductVariantMedia>> GetExistingVariantImagesAsync(ProductEntity product, CancellationToken cancellationToken)
        {
            return await _dbContext.ProductVariantMedias.Include(x => x.MediaFile).Include(x => x.AttributeValue)
                .Where(x => x.ProductVariant.ProductId == product.Id).ToListAsync(cancellationToken);
        }
        private async Task DeleteVariantImageAsync(ProductVariantMedia image, List<MediaFile> filesToDelete, CancellationToken cancellationToken)
        {
            if (image == null)
                return;

            var mediaFile = image.MediaFile;

            var isUsedElsewhere = mediaFile != null &&
                await _dbContext.ProductVariantMedias.AnyAsync(
                        x => x.MediaFileId == mediaFile.Id && x.Id != image.Id, cancellationToken);

            _dbContext.ProductVariantMedias.Remove(image);

            if (mediaFile == null || isUsedElsewhere)
                return;

            _dbContext.MediaFiles.Remove(mediaFile);
            filesToDelete.Add(mediaFile);
        }
            private async Task AddNewVariantImageAsync(ProductEntity product, ProductVariantImageRequest request, CancellationToken cancellationToken)
            {
                if (request.File == null)
                    return;

                var parts = request.Key.Split(':', 2, StringSplitOptions.TrimEntries);

                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                    throw new InvalidOperationException($"Variant image key '{request.Key}' không hợp lệ.");

                var attributeName = parts[0];
                var valueName = parts[1];

                //FIND ATTRIBUTE
                var attribute = await _dbContext.Attributes
                    .FirstOrDefaultAsync(x => x.Code == attributeName || x.Name == attributeName, cancellationToken);

                if (attribute == null)
                    throw new InvalidOperationException($"Không tìm thấy Attribute '{attributeName}'.");

                //FIND ATTRIBUTE VALUE
                var attributeValue = product.Variants.Where(x => x.IsActive)
                .SelectMany(x => x.VariantAttributes).Select(x => x.AttributeValue)
                .FirstOrDefault(x => x.AttributeId == attribute.Id && string.Equals(x.Value, valueName, StringComparison.OrdinalIgnoreCase));

                if (attributeValue == null)
                    throw new InvalidOperationException($"Không tìm thấy AttributeValue '{valueName}'.");

                //FIND ALL VARIANTS
                var variants = product.Variants
                .Where(x => x.IsActive && x.VariantAttributes.
                Any(va => va.AttributeValueId == attributeValue.Id)).ToList();

                if (variants.Count == 0)
                    throw new InvalidOperationException($"Không tìm thấy variant chứa '{request.Key}'.");

                //SAVE MEDIA ONCE
                var mediaFile = await SaveMediaFileAsync(request.File, cancellationToken);

                //CREATE MAPPING FOR ALL VARIANTS
                foreach (var variant in variants)
                {
                    var variantMedia = new ProductVariantMedia
                    {
                        DisplayOrder = 0,

                        ProductVariant = variant,

                        AttributeValue = attributeValue,

                        MediaFile = mediaFile
                    };

                    variant.VariantMedias.Add(variantMedia);
                }
            }
        private async Task ReplaceVariantImageAsync(ProductVariantMedia image, IFormFile file, List<MediaFile> filesToDelete, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return;

            var oldMediaFile = image.MediaFile;

            var productVariant = image.ProductVariant;
            var attributeValue = image.AttributeValue;
            var displayOrder = image.DisplayOrder;

            var newMediaFile = await SaveMediaFileAsync( file, cancellationToken);

            //REMOVE OLD MAPPING
            _dbContext.ProductVariantMedias.Remove(image);

            // CREATE NEW MAPPING
            var newVariantMedia = new ProductVariantMedia
            {
                ProductVariant = productVariant,
                AttributeValue = attributeValue,
                MediaFile = newMediaFile,
                DisplayOrder = displayOrder
            };

            productVariant.VariantMedias.Add(newVariantMedia);

            //DELETE OLD MEDIA
            if (oldMediaFile != null)
            {
                _dbContext.MediaFiles.Remove(oldMediaFile);
                filesToDelete.Add(oldMediaFile);
            }
        }

        private async Task<ServiceResult<int>> ValidateUpdateProductAsync(ProductEntity product, ProductRequest request, CancellationToken cancellationToken)
        {
            var sku = request.SKU.Trim();

            var skuExists = await _dbContext.Products.AnyAsync(x => x.Id != product.Id && x.Sku == sku, cancellationToken);

            if (skuExists)
                return ServiceResult<int>.Fail("SKU đã tồn tại.");

            var slug = request.Slug.Trim();

            var slugExists = await _dbContext.Products.AnyAsync(x => x.Id != product.Id && x.Slug == slug, cancellationToken);

            if (slugExists)
                return ServiceResult<int>.Fail("Slug đã tồn tại.");

            return ServiceResult<int>.Success(product.Id);
        }
        #endregion Update product

        public async Task<ServiceResult> MoveToDraftAsync(DeleteFormRequest request, CancellationToken cancellationToken)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
                return ServiceResult.Fail("Product", "Không tìm thấy sản phẩm.");

            product.Status = ProductStatus.Draft;
            product.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<ProductStatus>> ToggleSuspendAsync(int productId, CancellationToken cancellationToken)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);

            if (product == null)
                return ServiceResult<ProductStatus>.Fail("Product not found.");

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

        private async Task UpdateTagsAsync(ProductEntity product, string? tags, CancellationToken cancellationToken)
        {
            var tagValues = string.IsNullOrWhiteSpace(tags) ? [] : JsonSerializer.Deserialize<List<string>>(tags) ?? [];

            var tagNames = tagValues.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            //LẤY TAG ĐANG GẮN VỚI PRODUCT
            var oldMappings = await _dbContext.ProductTagMappings.Where(x => x.ProductId == product.Id).ToListAsync(cancellationToken);

            var oldTagIds = oldMappings.Select(x => x.TagId).ToHashSet();

            //KHÔNG CÓ TAG MỚI
            if (tagNames.Count == 0)
            {
                if (oldMappings.Count > 0)
                {
                    _dbContext.ProductTagMappings.RemoveRange(oldMappings);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                return;
            }

            //LẤY PRODUCT TAG ĐÃ TỒN TẠI
            var existingTags = await _dbContext.ProductTags.Where(x => tagNames.Contains(x.Name)).ToListAsync(cancellationToken);

            var tagsByName = existingTags.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            //TẠO PRODUCT TAG MỚI NẾU CHƯA CÓ
            foreach (var tagName in tagNames)
            {
                if (tagsByName.ContainsKey(tagName))
                    continue;

                var tag = new ProductTag
                {
                    Name = tagName,
                    Slug = await GenerateUniqueSlugCategoryAsync(tagName)
                };

                await _dbContext.ProductTags.AddAsync(tag, cancellationToken);

                tagsByName[tagName] = tag;
            }

            //SAVE TAG MỚI ĐỂ CÓ TagId
            var hasNewTags = tagsByName.Values.Any(x => x.Id == 0);

            if (hasNewTags)
                await _dbContext.SaveChangesAsync(cancellationToken);

            //TAG ID MỚI
            var newTagIds = tagsByName.Values.Select(x => x.Id).ToHashSet();

            //XÓA MAPPING CŨ KHÔNG CÒN ĐƯỢC CHỌN
            var mappingsToRemove = oldMappings.Where(x => !newTagIds.Contains(x.TagId)).ToList();

            if (mappingsToRemove.Count > 0)
            {
                _dbContext.ProductTagMappings.RemoveRange(
                    mappingsToRemove);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // CẬP NHẬT DISPLAY ORDER CHO MAPPING ĐÃ TỒN TẠI
            var currentMappings = oldMappings.Where(x => newTagIds.Contains(x.TagId)).ToList();
            foreach (var mapping in currentMappings)
            {
                var tag = tagsByName.Values.FirstOrDefault(x => x.Id == mapping.TagId);

                if (tag == null)
                    continue;

                var displayOrder = tagNames.FindIndex(x => string.Equals(x, tag.Name, StringComparison.OrdinalIgnoreCase));
                if (displayOrder >= 0)
                    mapping.DisplayOrder = displayOrder;
            }
            //THÊM MAPPING MỚI CHƯA TỒN TẠI
            var mappingsToAdd = tagNames
                .Select((tagName, index) =>
                {
                    var tag = tagsByName[tagName];

                    return new
                    {
                        TagId = tag.Id,
                        DisplayOrder = index
                    };
                }).Where(x => !oldTagIds.Contains(x.TagId))
                .Select(x => new ProductTagMapping
                {
                    ProductId = product.Id,
                    TagId = x.TagId,
                    DisplayOrder = x.DisplayOrder
                }).ToList();

            if (mappingsToAdd.Count > 0)
            {
                await _dbContext.ProductTagMappings.AddRangeAsync(mappingsToAdd, cancellationToken);
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

        private async Task ValidateVariantSkusAsync(int productId, ICollection<ProductVariantRequest> requests, CancellationToken cancellationToken = default)
        {
            if (requests == null || requests.Count == 0)
                return;

            var items = requests
                .Select(x => new
                {
                    x.Id,
                    Sku = x.SKU?.Trim()
                }).ToList();

            // CHECK EMPTY
            if (items.Any(x => string.IsNullOrWhiteSpace(x.Sku)))
                throw new InvalidOperationException("SKU variant không được để trống.");

            // CHECK DUPLICATE INSIDE REQUEST
            var duplicateSkus = items.GroupBy(x => x.Sku!, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1)
                .Select(x => x.Key).ToList();

            if (duplicateSkus.Count > 0)
                throw new InvalidOperationException($"SKU variant bị trùng: {string.Join(", ", duplicateSkus)}.");

            var requestSkus = items.Select(x => x.Sku!).ToList();

            // LOAD EXISTING ACTIVE VARIANTS
            var existingVariants = await _dbContext.ProductVariants
                .Where(x => x.ProductId == productId && x.IsActive && requestSkus.Contains(x.Sku))
                .Select(x => new
                {
                    x.Id,
                    x.Sku
                }).ToListAsync(cancellationToken);

            // CHECK CONFLICT
            foreach (var item in items)
            {
                var conflict = existingVariants.Any(x => x.Id != item.Id && string.Equals(x.Sku, item.Sku, StringComparison.OrdinalIgnoreCase));

                if (conflict)
                    throw new InvalidOperationException($"SKU variant '{item.Sku}' đã tồn tại trong sản phẩm.");
            }
        }
        
        private static ProductVariant BuildVariant(ProductEntity product, ProductVariantRequest request, int displayOrder = 0)
        {
            return new ProductVariant
            {
                ProductId = product.Id,

                Name = BuildVariantName(request.Options),

                Sku = request.SKU.Trim(),

                Price = request.Price,

                IsDefault = false,

                IsActive = true,

                DisplayOrder = displayOrder,

                CreatedAt = DateTime.UtcNow,

                VariantAttributes = [],

                VariantMedias = []
            };
        }
        private async Task AddVariantAttributesAsync(ProductVariant variant, Dictionary<string, string>? options, CancellationToken cancellationToken = default)
        {
            if (options == null || options.Count == 0)
                return;

            var attributeNames = options.Keys.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (attributeNames.Count == 0)
                return;

            var attributes = await _dbContext.Attributes.Include(x => x.Values)
                .Where(x => attributeNames.Contains(x.Code)).ToListAsync(cancellationToken);

            foreach (var option in options)
            {
                var attributeName = option.Key?.Trim();
                var valueName = option.Value?.Trim();

                if (string.IsNullOrWhiteSpace(attributeName) || string.IsNullOrWhiteSpace(valueName))
                    continue;

                var attribute = attributes.FirstOrDefault(x =>
                    string.Equals(x.Code, attributeName, StringComparison.OrdinalIgnoreCase));

                if (attribute == null)
                    throw new InvalidOperationException($"Không tìm thấy Attribute '{attributeName}'.");

                var attributeValue = attribute.Values.FirstOrDefault(x =>
                    string.Equals(x.Value, valueName, StringComparison.OrdinalIgnoreCase));

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

        #region Caterogy 
        public async Task<PagedResult<ProductCategoryListResult>> GetCategoryListAsync(DataTableResponse request)
        {
            var query = _dbContext.ProductCategories.AsNoTracking().Where(x => !x.IsDeleted);

            // Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%"));
            }

            var filteredCount = await query.CountAsync();

            query = query.Where(x => !x.IsDeleted).OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ThenBy(x => x.Name);

            var totalCount = await _dbContext.ProductCategories.CountAsync();

            var items = await query.Skip(request.Start).Take(request.Length)
                .Select(x => new ProductCategoryListResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Slug = x.Slug,
                    IsActive = x.IsActive,
                    ProductCount = x.Products.Count(p => !p.IsDeleted)
                }).ToListAsync();

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
                return await CreateProductCategoryAsync(model, cancellationToken);

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
                return ServiceResult<int>.Fail($"Name: không tồn tại.");

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

        public async Task<ServiceResult<int>> DeleteCategoryAsync(DeleteFormRequest request)
        {
            var category = await _dbContext.ProductCategories.FirstOrDefaultAsync(x => x.Id == request.Id);

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
