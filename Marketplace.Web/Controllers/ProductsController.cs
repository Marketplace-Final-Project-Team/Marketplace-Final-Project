using System.Security.Claims;
using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Marketplace.Web.Models.Products;
using Microsoft.AspNetCore.Authorization;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace Marketplace.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IBusinessAccessService _businessAccessService;
    private readonly IActivityLogService _activityLog;
    private readonly ISystemSettingsService _settingsService;
    private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

    public ProductsController(
        IProductService productService,
        IGenericRepository<Category> categoryRepository,
        IBusinessAccessService businessAccessService,
        IActivityLogService activityLog,
        ISystemSettingsService settingsService,
        IStringLocalizer<SharedResource> sharedLocalizer)
    {
        _productService = productService;
        _categoryRepository = categoryRepository;
        _businessAccessService = businessAccessService;
        _activityLog = activityLog;
        _settingsService = settingsService;
        _sharedLocalizer = sharedLocalizer;
    }

    public async Task<IActionResult> Index(Guid? businessId)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        var businesses = await _businessAccessService.GetAccessibleBusinessesAsync(userId, BusinessPermissions.ProductCrud);
        if (businesses.Count == 0)
        {
            TempData["Error"] = "У вас поки немає бізнесу з доступом до керування товарами.";
            ViewBag.Businesses = Array.Empty<SelectListItem>();
            return View(new List<ProductListItemViewModel>());
        }

        var selectedBusinessId = businessId ?? businesses[0].Id;
        if (!await _businessAccessService.HasAccessAsync(selectedBusinessId, userId, BusinessPermissions.ProductCrud))
        {
            return Forbid();
        }

        var products = await _productService.GetByBusinessAsync(selectedBusinessId);
        ViewBag.Businesses = businesses.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = b.StoreName,
            Selected = b.Id == selectedBusinessId
        }).ToList();
        ViewBag.SelectedBusinessId = selectedBusinessId;

        return View(products.Select(p =>
        {
            var activeDiscount = p.GetActiveProductDiscount();

            return new ProductListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Quantity = p.Quantity,
                Sku = p.Sku,
                VariantCount = p.Variants.Count,
                MainImageUrl = p.MainImageUrl,
                Status = p.Status.ToString(),
                CategoryName = p.Category?.Name ?? string.Empty,
                ModerationReason = p.ModerationReason,
                HasDiscount = activeDiscount != null,
                DiscountPercent = activeDiscount != null ? (int)activeDiscount.Percent : 0
            };
        }).ToList());
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var accessible = await GetAccessibleProductAsync(id);
        if (accessible.Result is not null) return accessible.Result;

        var product = accessible.Product!;
        return View(new ProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            Sku = product.Sku,
            Variants = product.Variants
                .OrderBy(v => v.Size)
                .Select(v => new ProductVariantInputModel { Size = v.Size, Sku = v.Sku, Quantity = v.Quantity })
                .ToList(),
            MainImageUrl = product.MainImageUrl,
            Status = product.Status.ToString(),
            CategoryName = product.Category?.Name ?? "Без категорії",
            ModerationReason = product.ModerationReason,
            AdditionalImageUrls = product.Images.Select(i => i.ImageUrl).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? businessId)
    {
        var model = new ProductCreateViewModel
        {
            BusinessId = businessId ?? Guid.Empty
        };
        await PopulateCreateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        var hasAccess = model.BusinessId != Guid.Empty
            && await _businessAccessService.HasAccessAsync(model.BusinessId, userId, BusinessPermissions.ProductCrud);
        if (!hasAccess) return Forbid();

        if (!await _categoryRepository.ExistsAsync(category => category.Id == model.CategoryId))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Оберіть доступну категорію.");
        }

        var maxProducts = await _settingsService.GetIntAsync("BusinessRules", "MaxProductsPerBusiness", 200);
        var currentProductCount = (await _productService.GetByBusinessAsync(model.BusinessId)).Count;
        if (currentProductCount >= maxProducts)
        {
            ModelState.AddModelError(string.Empty, $"Досягнуто максимальної кількості товарів для одного бізнесу ({maxProducts}).");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateOptionsAsync(model);
            return View(model);
        }

        ProductCreateDto createdProductDto;
        try
        {
            createdProductDto = new ProductCreateDto
            {
                BusinessId = model.BusinessId,
                CategoryId = model.CategoryId,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Quantity = model.Quantity,
                Sku = model.Sku,
                Variants = model.Variants
                    .Where(v => !string.IsNullOrWhiteSpace(v.Size))
                    .Select(v => new ProductVariantDto { Size = v.Size, Sku = v.Sku, Quantity = v.Quantity })
                    .ToList(),
                IsAdultOnly = model.IsAdultOnly,
                MainImage = model.MainImage,
                AdditionalImages = model.AdditionalImages
            };

            await _productService.CreateAsync(createdProductDto);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.IsAdultOnly), ex.Message);
            await PopulateCreateOptionsAsync(model);
            return View(model);
        }

        await _activityLog.LogBusinessActionAsync(model.BusinessId, "ProductCreated",
            $"Додано товар «{createdProductDto.Name}».", actorUserId: userId);

        TempData["Success"] = "Товар створено та відправлено на модерацію.";
        return RedirectToAction(nameof(Index), new { businessId = model.BusinessId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var accessible = await GetAccessibleProductAsync(id);
        if (accessible.Result is not null) return accessible.Result;

        var product = accessible.Product!;

        ViewBag.ActiveDiscount = product.GetActiveProductDiscount();

        var userId = GetCurrentUserId();
        var businesses = userId is null
            ? Array.Empty<Business>()
            : await _businessAccessService.GetAccessibleBusinessesAsync(userId, BusinessPermissions.ProductCrud);

        var currentBusiness = businesses.FirstOrDefault(b => b.Id == product.BusinessId);
        bool isAdultApproved = currentBusiness?.AdultStatus == AdultContentStatus.Approved;

        return View(new ProductEditViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            Sku = product.Sku,
            Variants = product.Variants
                .OrderBy(v => v.Size)
                .Select(v => new ProductVariantInputModel { Size = v.Size, Sku = v.Sku, Quantity = v.Quantity })
                .ToList(),
            IsAdultOnly = product.IsAdultOnly,
            CategoryId = product.CategoryId,
            ExistingMainImageUrl = product.MainImageUrl,
            Categories = await BuildCategoryOptionsAsync(product.CategoryId, isAdultApproved)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var accessible = await GetAccessibleProductAsync(id);
        if (accessible.Result is not null) return accessible.Result;

        if (!await _categoryRepository.ExistsAsync(category => category.Id == model.CategoryId))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Оберіть доступну категорію.");
        }

        if (!ModelState.IsValid)
        {
            var userId = GetCurrentUserId();
            var businesses = userId is null
                ? Array.Empty<Business>()
                : await _businessAccessService.GetAccessibleBusinessesAsync(userId, BusinessPermissions.ProductCrud);
            var currentBusiness = businesses.FirstOrDefault(b => b.Id == accessible.Product!.BusinessId);
            bool isAdultApproved = currentBusiness?.AdultStatus == AdultContentStatus.Approved;

            model.Categories = await BuildCategoryOptionsAsync(model.CategoryId, isAdultApproved);
            return View(model);
        }

        try
        {
            var updated = await _productService.UpdateAsync(new ProductEditDto
            {
                Id = model.Id,
                CategoryId = model.CategoryId,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Quantity = model.Quantity,
                Sku = model.Sku,
                Variants = model.Variants
                    .Where(v => !string.IsNullOrWhiteSpace(v.Size))
                    .Select(v => new ProductVariantDto { Size = v.Size, Sku = v.Sku, Quantity = v.Quantity })
                    .ToList(),
                IsAdultOnly = model.IsAdultOnly,
                MainImage = model.MainImage,
                AdditionalImages = model.AdditionalImages
            });

            if (updated is null) return NotFound();

            await _activityLog.LogBusinessActionAsync(updated.BusinessId, "ProductEdited",
                $"Товар «{updated.Name}» відредаговано.", actorUserId: GetCurrentUserId(), relatedEntityId: updated.Id);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.IsAdultOnly), ex.Message);
            var userId = GetCurrentUserId();
            var businesses = userId is null
                ? Array.Empty<Business>()
                : await _businessAccessService.GetAccessibleBusinessesAsync(userId, BusinessPermissions.ProductCrud);
            var currentBusiness = businesses.FirstOrDefault(b => b.Id == accessible.Product!.BusinessId);
            bool isAdultApproved = currentBusiness?.AdultStatus == AdultContentStatus.Approved;

            model.Categories = await BuildCategoryOptionsAsync(model.CategoryId, isAdultApproved);
            return View(model);
        }

        TempData["Success"] = "Товар оновлено та відправлено на повторну модерацію.";
        return RedirectToAction(nameof(Index), new { businessId = accessible.Product!.BusinessId });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var accessible = await GetAccessibleProductAsync(id);
        if (accessible.Result is not null) return accessible.Result;

        var product = accessible.Product!;
        return View(new ProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            MainImageUrl = product.MainImageUrl,
            Status = product.Status.ToString(),
            CategoryName = product.Category?.Name ?? "Без категорії"
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var accessible = await GetAccessibleProductAsync(id);
        if (accessible.Result is not null) return accessible.Result;

        await _productService.DeleteAsync(id);
        TempData["Success"] = "Товар видалено.";
        return RedirectToAction(nameof(Index), new { businessId = accessible.Product!.BusinessId });
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task PopulateCreateOptionsAsync(ProductCreateViewModel model)
    {
        var userId = GetCurrentUserId();
        var businesses = userId is null
            ? Array.Empty<Business>()
            : await _businessAccessService.GetAccessibleBusinessesAsync(userId, BusinessPermissions.ProductCrud);

        model.Businesses = businesses.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = b.StoreName,
            Selected = b.Id == model.BusinessId
        }).ToList();

        var currentBusiness = businesses.FirstOrDefault(b => b.Id == model.BusinessId);
        bool isAdultApproved = currentBusiness?.AdultStatus == AdultContentStatus.Approved;

        model.Categories = await BuildCategoryOptionsAsync(model.CategoryId, isAdultApproved);
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildCategoryOptionsAsync(Guid selectedId, bool allowAdult = true)
    {
        var categories = await _categoryRepository.GetAllAsync();

        if (!allowAdult)
        {
            categories = categories.Where(c => !c.IsAdultOnly).ToList();
        }

        return categories.OrderBy(category => category.Name).Select(category => new SelectListItem
        {
            Value = category.Id.ToString(),
            Text = category.IsAdultOnly ? $"{_sharedLocalizer[category.Name]} (18+)" : _sharedLocalizer[category.Name],
            Selected = category.Id == selectedId
        }).ToList();
    }

    private async Task<(Product? Product, IActionResult? Result)> GetAccessibleProductAsync(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return (null, Challenge());

        var product = await _productService.GetByIdAsync(id);
        if (product is null) return (null, NotFound());

        var hasAccess = await _businessAccessService.HasAccessAsync(
            product.BusinessId,
            userId,
            BusinessPermissions.ProductCrud);
        return hasAccess ? (product, null) : (null, Forbid());
    }

}