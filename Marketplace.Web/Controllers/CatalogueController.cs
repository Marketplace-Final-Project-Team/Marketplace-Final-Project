using System.Security.Claims;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Controllers;

[AllowAnonymous]
[Route("Catalog")]
[Route("Catalogue")]
public class CatalogueController : Controller
{
    private const int PageSize = 12;

    private readonly IProductService _productService;
    private readonly IReviewService _reviewService;
    private readonly ICategoryService _categoryService;
    private readonly IWishlistService _wishlistService;

    public CatalogueController(
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
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(CatalogFilterViewModel filter)
    {
        filter.Page = filter.Page < 1 ? 1 : filter.Page;

        var categories = await _categoryService.GetAllCategories();

        bool allowAdultContent = Request.Cookies["AdultConfirmed"] == "true";

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
        }, allowAdultContent: allowAdultContent);

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

        return View("~/Views/Catalog/Index.cshtml", model);
    }

    [HttpGet]
    [Route("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        bool allowAdultContent = Request.Cookies["AdultConfirmed"] == "true";

        var product = await _productService.GetCatalogProductAsync(id, allowAdultContent);
        if (product is null)
        {
            // Якщо товар 18+, а користувач ще не підтверджував вік
            var rawProduct = await _productService.GetByIdAsync(id);
            if (rawProduct != null && rawProduct.IsAdultOnly)
            {
                // Перенаправляємо на сторінку підтвердження віку
                return View("~/Views/Catalog/AgeVerification.cshtml", id);
            }

            return NotFound();
        }

        var model = MapProduct(product);
        var catalog = await _productService.GetCatalogAsync(allowAdultContent: allowAdultContent);

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

        return View("~/Views/Catalog/Details.cshtml", model);
    }

    [HttpPost]
    [Route("ConfirmAge")]
    public IActionResult ConfirmAge(Guid? productId, string? returnUrl)
    {
        Response.Cookies.Append("AdultConfirmed", "true", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        });

        if (productId.HasValue && productId != Guid.Empty)
        {
            return RedirectToAction(nameof(Details), new { id = productId.Value });
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("SearchSuggestions")]
    public async Task<IActionResult> SearchSuggestions(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Json(Array.Empty<object>());
        }

        bool allowAdultContent = Request.Cookies["AdultConfirmed"] == "true";
        var suggestions = await _productService.GetSearchSuggestionsAsync(
            term: term,
            limit: 5,
            allowAdultContent: allowAdultContent
        );

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
            IsAgeRestricted = product.IsAdultOnly
        };
    }
}