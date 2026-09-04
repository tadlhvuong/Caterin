using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Catelog;
using Shared.Data.Entities.Product;
using Shared.Enums;
using Shared.Interfaces.Core;
using Shared.Interfaces.Log;
using Shared.Requests;
using Shared.Requests.Product;
using Shared.Requests.Product.Category;
using Shared.Services.Log;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Website.Areas.Admin.Models.Product;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/product")]
    [PermissionModule("Products")]
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private readonly IActivityLogger _activityLogger;

        private readonly IProductService _productService;

        private readonly AppDbContext _dbContext;
        public ProductController(ILogger<ProductController> logger, IActivityLogger activityLogger, AppDbContext dbContext, IProductService productService) 
        {
            _logger = logger;
            _activityLogger = activityLogger;
            _productService = productService;

            _dbContext = dbContext;
        }
        // GET: HomeController1
        [HttpGet]
        [PermissionAction(ActionType.View)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost("get-products")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetProducts([FromBody] DataTableRequest request)
        {
            var result = await _productService.GetProductsAsync(request);

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                data = result.Items
            });
        }

        [HttpGet("statuses")]
        [PermissionAction(ActionType.View)]
        public IActionResult GetStatuses()
        {
            var statuses = Enum.GetValues<ProductStatus>()
                .Select(x =>
                {
                    var member = typeof(ProductStatus)
                        .GetMember(x.ToString())
                        .First();

                    var display = member
                        .GetCustomAttribute<DisplayAttribute>();

                    return new
                    {
                        value = (int)x,
                        name = display?.GetShortName() ?? x.ToString()
                    };
                });

            return Ok(statuses);
        }

        //[HttpPost("update-stock-status")]
        //[PermissionAction(ActionType.Edit)]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UpdateStockStatus([FromBody] UpdateProductStockRequest request)
        //{
        //    var result = await _productService.UpdateStockStatusAsync(request);

        //    if (!result.Succeeded)
        //    {
        //        foreach (var error in result.Errors)
        //        {
        //            ModelState.AddModelError(error.Field ?? string.Empty,
        //                error.Message);
        //        }

        //        return ValidationProblem(ModelState);
        //    }

        //    return Ok(new
        //    {
        //        success = true,
        //        message = "Stock status đã được cập nhật.",
        //        data = result.Data
        //    });
        //}

        // GET: HomeController1/Details/5
        [HttpGet("details/{id}")]
        [PermissionAction(ActionType.View)]

        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: HomeController1/Edit/5
        [HttpGet("create")]
        [PermissionAction(ActionType.Create)]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost("create")]
        [PermissionAction(ActionType.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] CreateProductRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var existsSKU = await _productService.ExistsBySKUProductAsync(request.SKU);
            if (existsSKU)
            {
                ModelState.AddModelError(nameof(request.SKU), $"{request.SKU} đã tồn tại.");
                return ValidationProblem(ModelState);
            }

            var existsSlug = await _productService.ExistsBySlugProductAsync(request.Slug);
            if (existsSlug)
            {
                ModelState.AddModelError(nameof(request.Slug), $"{request.Slug} đã tồn tại.");
                return ValidationProblem(ModelState);
            }
            //if (request.VariantImages.Count != 0 || request.Variants.Length != 0)
            //{
            //    if (request.Variants.Length == 0)
            //    {
            //        return Json(new
            //        {
            //            success = false,
            //            message = "Cập nhật sản phẩm thất bại.",
            //            redirectUrl = Url.Action(nameof(Index), "Product", new { area = "Admin" })
            //        });
            //    }
            //    if (request.VariantImages.Count == 0)
            //    {
            //        return Json(new
            //        {
            //            success = false,
            //            message = "Cập nhật sản phẩm thất bại.",
            //            redirectUrl = Url.Action(nameof(Index), "Product", new { area = "Admin" })
            //        });
            //    }

            //}
            var result = await _productService.CreateAsync(request, cancellationToken);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {

                    ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
                }
                return ValidationProblem(ModelState);
            }

            if (request.Action == "Draft")
            {
                TempData["Success"] = "Đã lưu sản phẩm vào bản nháp.";
            }
            else
            {
                TempData["Success"] = "Sản phẩm đã được đăng.";
            }

            //return Json(new
            //{
            //    success = true,
            //    message = model.Id == 0 ? "Category created successfully." : "Category updated successfully.",
            //    id = result.Data
            //});
            return Json(new
            {
                success = true,
                message = TempData["Success"],
                redirectUrl = Url.Action(nameof(Index), "Product", new { area = "Admin" })
            });
        }
        [HttpGet("get-create-product-categories")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetCreateProductCategories(CancellationToken cancellationToken)
        {
            var categories = await _productService.GetCreateProductCategoriesAsync(cancellationToken);

            return Json(new
            {
                success = true,
                data = categories
            });
        }

        [HttpGet("get-create-attributes")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetCreateAttributes(CancellationToken cancellationToken)
        {
            var attributes = await _productService.GetCreateAttributesAsync(cancellationToken);

            return Json(new
            {
                success = true,
                data = attributes
            });
        }
        // GET: HomeController1/Edit/5
        [HttpGet("edit/{id:int}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> Update(
    int id,
    CancellationToken cancellationToken)
        {
            var model = new CreateProductRequest
            {
                Id = id
            };


            return View(model);
        }

        [HttpGet("get-update-product")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetUpdateProduct(
    int id,
    CancellationToken cancellationToken)
        {
            var result = await _productService.GetUpdateProductAsync(id, cancellationToken);

            if (!result.Succeeded)
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        [HttpPost("edit")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> Update(
    [FromForm] CreateProductRequest request,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // =========================================================
            // UPDATE PRODUCT
            // =========================================================
            //if(request.VariantImages.Count != 0 ||  request.Variants.Length  != 0)
            //{
            //    if(request.Variants.Length == 0)
            //    {
            //        return Json(new
            //        {
            //            success = false,
            //            message = "Dữ liệu biến thể lỗi. Thử lại sau",
            //            redirectUrl =  ""
            //        });
            //    }
            //    if(request.VariantImages.Count == 0)
            //    {
            //        return Json(new
            //        {
            //            success = false,
            //            message = "Dữ liệu ảnh biến thể lỗi. Thử lại sau",
            //            redirectUrl = ""
            //        });
            //    }    

            //}    
            var result = await _productService.UpdateAsync(request, cancellationToken);

            // =========================================================
            // BUSINESS ERROR
            // =========================================================
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {

                    ModelState.AddModelError(
                        error.Field ?? string.Empty,
                        error.Message);
                }
                return ValidationProblem(ModelState);
            }
            if (request.Action == "Draft")
            {
                TempData["Success"] = "Đã lưu sản phẩm vào bản nháp.";
            }
            else
            {
                TempData["Success"] = "Sản phẩm đã được đăng.";
            }
            return Json(new
            {
                success = true,
                message = result.Message ?? "Cập nhật sản phẩm thành công.",
                redirectUrl = Url.Action(nameof(Index), "Product", new { area = "Admin" })
            });
        }
        //[HttpPost("edit/{id:int}")]
        //[PermissionAction(ActionType.Edit)]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Update(int id, [FromForm] CreateProductRequest request, CancellationToken cancellationToken)
        //{
        //    if (!ModelState.IsValid)
        //        return ValidationProblem(ModelState);

        //    return RedirectToAction(nameof(Index));
        //}

        // GET: HomeController1/Delete/5
        [HttpGet("delete/{id}")]
        [PermissionAction(ActionType.Delete)]
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: HomeController1/Delete/5
        [HttpPost("delete")]
        [PermissionAction(ActionType.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] DeleteFormRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.MoveToDraftAsync(request, cancellationToken);

            if (!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    message = result.Errors.FirstOrDefault()?.Message ?? "Không thể chuyển sản phẩm về bản nháp."
                });
            }

            return Json(new
            {
                success = true,
                message = "Đã chuyển sản phẩm về bản nháp."
            });
        }

        [HttpPost("toggle-suspend")]
        [PermissionAction(ActionType.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSuspend([FromBody] DeleteFormRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.ToggleSuspendAsync(request.Id, cancellationToken);

            if (!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Json(new
            {
                success = true,
                message = result.Message,
                status = result.Data
            });
        }


        [HttpGet("product-category")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> ProductCategory(int id = 0)
        {
            var model = new ProductCategoryViewModel
            {
                Statuses =
                    [
                        new SelectListItem
                        {
                            Value = "true",
                            Text = "Active"
                        },
                        new SelectListItem
                        {
                            Value = "false",
                            Text = "Inactive"
                        }
                    ],
                CreateProduct = new EditProductCategoryRequest()
            };
            ViewData["Statuses"] = new List<SelectListItem>
                   {
                        new SelectListItem
                        {
                            Value = "true",
                            Text = "Active"
                        },
                        new SelectListItem
                        {
                            Value = "false",
                            Text = "Inactive"
                        }
            };
            return View(model.CreateProduct);
       
        }

        [HttpPost("edit-product-category")]
        [ValidateAntiForgeryToken]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> EditProductCategory(EditProductCategoryRequest model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);


            if(model.Id  == 0)
            {
                var existsSlug = await _productService.ExistsBySlugCategoryAsync(model.Slug);
                if (existsSlug)
                {
                    ModelState.AddModelError(nameof(model.Slug), $"{model.Slug} đã tồn tại.");
                    return ValidationProblem(ModelState);
                }

            }
            var result = await _productService.SaveCategoryAsync(model, cancellationToken);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {

                    ModelState.AddModelError(
                        error.Field ?? string.Empty,
                        error.Message);
                }
                return ValidationProblem(ModelState);
            }
            return Json(new
            {
                success = true,
                message = model.Id == 0 ? "Category created successfully." : "Category updated successfully.",
                id = result.Data
            });
        }

        [HttpPost("delete-category")]
        [PermissionAction(ActionType.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory([FromBody] DeleteFormRequest request)
        {
            if (request.Id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Category ID không hợp lệ."
                });
            }

            var result = await _productService.DeleteAsync(request);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Errors.FirstOrDefault()?.Message ?? "Không thể xóa category."
                });

            }
            var name = _dbContext.ProductCategories.FirstOrDefault(x => x.Id == result.Data)?.Name;
            return Ok(new
            {
                success = true,
                message = name + " đã được xóa thành công.",
                data = result.Data
            });
        }

        [HttpGet("get-category/{id:int}")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _productService.GetCategoryIdAsync(id);

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = category.Id,
                    name = category.Name,
                    slug = category.Slug,
                    description = category.Description,
                    seoTitle = category.SeoTitle,
                    seoDescription = category.SeoDescription,
                    seoKeyword = category.SeoKeywords,
                    isActive = category.IsActive
                }
            });
        }
        [HttpPost("get-categories")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GetCategoryList([FromBody] DataTableRequest request)
        {
            var result = await _productService.GetCategoryListAsync(request);

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                data = result.Items
            });
        }

        [HttpGet("generate-slug-product")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GenerateSlugProduct(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Category name is required."
                });
            }

            var slug = await _productService.GenerateUniqueSlugProductAsync(name);

            return Json(new
            {
                success = true,
                slug
            });
        }

        [HttpGet("generate-slug-category")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> GenerateSlugCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Category name is required."
                });
            }

            var slug = await _productService.GenerateUniqueSlugCategoryAsync(name);

            return Json(new
            {
                success = true,
                slug
            });
        }
    }
}
