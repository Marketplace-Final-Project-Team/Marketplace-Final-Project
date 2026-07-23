using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Web.Models.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;

        // ВРЕМЕННО
        private static readonly Guid TempBusinessId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        private static readonly Guid TempCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: /Products
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetByBusinessAsync(TempBusinessId);

            var model = products.Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Quantity = p.Quantity,
                MainImageUrl = p.MainImageUrl,
                Status = p.Status.ToString(),
                CategoryName = p.Category?.Name ?? "Тестовая категория"
            }).ToList();

            return View(model);
        }

        // GET: /Products/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();

            var model = new ProductDetailsViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                MainImageUrl = product.MainImageUrl,
                Status = product.Status.ToString(),
                CategoryName = product.Category?.Name ?? "Тестовая категория",
                AdditionalImageUrls = product.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
            };

            return View(model);
        }

        // GET: /Products/Create
        public IActionResult Create()
        {
            var model = new ProductCreateViewModel
            {
                Categories = new List<SelectListItem>
                {
                    new SelectListItem { Value = TempCategoryId.ToString(), Text = "Тестовая категория" }
                }
            };
            return View(model);
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = new List<SelectListItem>
                {
                    new SelectListItem { Value = TempCategoryId.ToString(), Text = "Тестовая категория" }
                };
                return View(model);
            }

            var dto = new ProductCreateDto
            {
                //!!!!!! заменить на business.Id после подключения авторизации!!!!!!!!!!!
                BusinessId = TempBusinessId,
                CategoryId = TempCategoryId,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Quantity = model.Quantity,
                MainImage = model.MainImage,
                AdditionalImages = model.AdditionalImages
            };

            await _productService.CreateAsync(dto);

            TempData["Success"] = "Товар створено та відправлено на модерацію.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();

            var model = new ProductEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                CategoryId = product.CategoryId,
                ExistingMainImageUrl = product.MainImageUrl,
                Categories = new List<SelectListItem>
                {
                    new SelectListItem { Value = TempCategoryId.ToString(), Text = "Тестовая категория" }
                }
            };

            return View(model);
        }

        // POST: /Products/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ProductEditViewModel model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                model.Categories = new List<SelectListItem>
                {
                    new SelectListItem { Value = TempCategoryId.ToString(), Text = "Тестовая категория" }
                };
                return View(model);
            }

            var dto = new ProductEditDto
            {
                Id = model.Id,
                CategoryId = model.CategoryId,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Quantity = model.Quantity,
                MainImage = model.MainImage,
                AdditionalImages = model.AdditionalImages
            };

            var updated = await _productService.UpdateAsync(dto);
            if (updated == null) return NotFound();

            TempData["Success"] = "Товар оновлено та відправлено на повторну модерацію.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Delete/{id}
        public async Task<IActionResult> Delete(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();

            var model = new ProductDetailsViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                MainImageUrl = product.MainImageUrl,
                Status = product.Status.ToString(),
                CategoryName = product.Category?.Name ?? "Тестовая категория"
            };

            return View(model);
        }

        // POST: /Products/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _productService.DeleteAsync(id);
            TempData["Success"] = "Товар видалено.";
            return RedirectToAction(nameof(Index));
        }
    }
}