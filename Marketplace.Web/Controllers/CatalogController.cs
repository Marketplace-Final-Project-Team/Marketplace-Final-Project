using System.Security.Claims;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Controllers;

[AllowAnonymous]
public class CatalogController : Controller
{
    private const int PageSize = 12;

    private readonly IProductService _productService;
    private readonly IReviewService _reviewService;
    private readonly ICategoryService _categoryService;
    private readonly IWishlistService _wishlistService;

    public CatalogController(
        IProductService productService,
        IReviewService reviewService,
        ICategoryService categoryService,
        IWishlistService wishlistService)
    {
        _productService = productService;
        _reviewService = reviewService;
        _categoryService = categoryService;
        _wishlistService = wishlistService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CatalogFilterViewModel filter)
    {
        filter.Page = filter.Page < 1 ? 1 : filter.Page;

        var categories = await _categoryService.GetAllCategories();

        var result = await _productService.GetCatalogPageAsync(new CatalogFilterDto
        {
            Search = filter.Search,
            CategoryId = filter.CategoryId,
            MinPrice = filter.MinPrice,
            MaxPrice = filter.MaxPrice,
            InStockOnly = filter.InStockOnly,
            MinRating = filter.MinRating,
            Sort = filter.Sort,
            Page = filter.Page,
            PageSize = PageSize
        });

        if (result.TotalPages > 0 && filter.Page > result.TotalPages)
        {
            filter.Page = result.TotalPages;
            return RedirectToAction(nameof(Index), filter);
        }

        var model = new CatalogPageViewModel
        {
            Filter = filter,
            Categories = categories
                .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
                .ToList(),
            Products = result.Items.Select(item => MapProduct(item.Product, item.AverageRating, item.ReviewCount)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var product = await _productService.GetCatalogProductAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var model = MapProduct(product);
        var catalog = await _productService.GetCatalogAsync();
        model.EstimatedDeliveryStart = DateTime.Today.AddDays(2);
        model.EstimatedDeliveryEnd = DateTime.Today.AddDays(4);
        model.RelatedProducts = catalog
            .Where(candidate => candidate.Id != product.Id)
            .OrderByDescending(candidate => candidate.CategoryId == product.CategoryId)
            .ThenByDescending(candidate => candidate.CreatedAt)
            .Take(4)
            .Select(candidate => new CatalogRelatedProductViewModel
            {
                Id = candidate.Id,
                Name = candidate.Name,
                CategoryName = candidate.Category?.Name ?? "Без категорії",
                MainImageUrl = candidate.MainImageUrl,
                Price = candidate.Price,
                Quantity = candidate.Quantity
            })
            .ToList();

        model.Reviews = (await _reviewService.GetReviewsByProductId(id)).ToList();
        model.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.ReviewCount = model.Reviews.Count;
        model.AverageRating = model.Reviews.Count == 0 ? 0 : Math.Round(model.Reviews.Average(r => r.Rating), 1);

        if (model.CurrentUserId is not null)
        {
            var wishlist = await _wishlistService.GetWishlistAsync(model.CurrentUserId);
            model.IsInWishlist = wishlist.Items.Any(item => item.ProductId == id);
        }

        return View(model);
    }

    // ЕНДПОЇНТ ДЛЯ АВТОДОПОВНЕННЯ В ПОШУКУ
    [HttpGet]
    public async Task<IActionResult> SearchSuggestions(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Json(Array.Empty<object>());
        }

        var catalog = await _productService.GetCatalogAsync();
        var suggestions = catalog
            .Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.GetDiscountedPrice()
            });

        return Json(suggestions);
    }

    private static CatalogProductViewModel MapProduct(Marketplace.Domain.Entities.Product product, double averageRating = 0, int reviewCount = 0)
    {
        var activeDiscount = product.GetActiveDiscount();

        return new CatalogProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = activeDiscount != null ? product.GetDiscountedPrice() : product.Price,
            Quantity = product.Quantity,
            MainImageUrl = product.MainImageUrl,
            CategoryName = product.Category?.Name ?? "Без категорії",
            StoreName = product.Business?.StoreName ?? "Marketplace",
            StoreLogoUrl = product.Business?.LogoUrl ?? string.Empty,
            StoreDescription = product.Business?.Description ?? string.Empty,
            AdditionalImageUrls = product.Images.Select(image => image.ImageUrl).ToList(),
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            OriginalPrice = activeDiscount != null ? product.Price : null,
            DiscountPercent = activeDiscount?.Percent,
     
        };
    }
}