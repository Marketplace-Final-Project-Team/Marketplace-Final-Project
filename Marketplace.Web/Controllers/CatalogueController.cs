using System.Security.Claims;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Enums;
using Marketplace.Web.Infrastructure;
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
    private readonly ISystemSettingsService _settingsService;
    private readonly IProductAnalyticsService _productAnalyticsService;

    public CatalogueController(
        IProductService productService,
        IReviewService reviewService,
        ICategoryService categoryService,
        IWishlistService wishlistService,
        ISystemSettingsService settingsService,
        IProductAnalyticsService productAnalyticsService)
    {
        _productService = productService;
        _reviewService = reviewService;
        _categoryService = categoryService;
        _wishlistService = wishlistService;
        _settingsService = settingsService;
        _productAnalyticsService = productAnalyticsService;
    }
    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    


    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(CatalogFilterViewModel filter)
    {
        filter.Page = filter.Page < 1 ? 1 : filter.Page;
        var userId = GetCurrentUserId();

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

        var selectedCategory = filter.CategoryId.HasValue
            ? categories.FirstOrDefault(category => category.Id == filter.CategoryId.Value)
            : null;

        var model = new CatalogPageViewModel
        {
            Filter = filter,
            Categories = categories
                .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
                .ToList(),
            Products = result.Items.Select(item => MapProduct(item.Product, item.AverageRating, item.ReviewCount, userId)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            SelectedCategoryName = selectedCategory?.Name,
            SelectedCategoryImageUrl = selectedCategory?.ImageUrl
        };

        return View("~/Views/Catalog/Index.cshtml", model);
    }

    [HttpGet]
    [Route("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        bool allowAdultContent = Request.Cookies["AdultConfirmed"] == "true";
        var userId = GetCurrentUserId();

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

        var model = MapProduct(product,0,0,userId);
        model.LowStockThreshold = await _settingsService.GetIntAsync("BusinessRules", "LowStockThreshold", 5);
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

        await _productAnalyticsService.RecordEventAsync(product.Id, product.BusinessId, ProductEventType.View, model.CurrentUserId);

        RecordRecentlyViewed(id);

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

    private void RecordRecentlyViewed(Guid productId)
    {
        if (!CookieConsent.IsAccepted(Request.Cookies))
        {
            return;
        }

        Request.Cookies.TryGetValue(RecentlyViewedCookie.Name, out var raw);
        var ids = RecentlyViewedCookie.Parse(raw);

        ids.RemoveAll(existingId => existingId == productId);
        ids.Insert(0, productId);
        if (ids.Count > RecentlyViewedCookie.MaxItems)
        {
            ids = ids.Take(RecentlyViewedCookie.MaxItems).ToList();
        }

        Response.Cookies.Append(RecentlyViewedCookie.Name, RecentlyViewedCookie.Serialize(ids), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(RecentlyViewedCookie.Lifetime),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = false
        });
    }

    [HttpGet]
    [Route("SearchSuggestions")]
    public async Task<IActionResult> SearchSuggestions(string? term)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return Json(new SearchSuggestionsResultViewModel());
        }

        bool allowAdultContent = Request.Cookies["AdultConfirmed"] == "true";
        var suggestions = await _productService.GetSearchSuggestionsAsync(
            term: term,
            limit: 5,
            allowAdultContent: allowAdultContent
        );

        var normalized = term.Trim();
        var catalog = await _productService.GetCatalogAsync(allowAdultContent: allowAdultContent);

        var matches = catalog
            .Where(product =>
                product.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || (product.Category?.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(product => product.Name.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(product => product.CreatedAt)
            .Take(6)
            .ToList();

        string? correctedTerm = null;

        if (matches.Count == 0)
        {
            var threshold = Math.Max(1, normalized.Length / 3);
            var fuzzy = catalog
                .Select(product => new
                {
                    Product = product,
                    Distance = product.Name
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(word => LevenshteinDistance(normalized, word))
                        .DefaultIfEmpty(int.MaxValue)
                        .Min()
                })
                .Where(candidate => candidate.Distance <= threshold)
                .OrderBy(candidate => candidate.Distance)
                .ThenByDescending(candidate => candidate.Product.CreatedAt)
                .Take(6)
                .ToList();

            if (fuzzy.Count > 0)
            {
                matches = fuzzy.Select(candidate => candidate.Product).ToList();
                correctedTerm = fuzzy[0].Product.Name
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .OrderBy(word => LevenshteinDistance(normalized, word))
                    .First();
            }
        }

        return Json(new SearchSuggestionsResultViewModel
        {
            Term = normalized,
            CorrectedTerm = correctedTerm,
            Items = matches.Select(product => new SearchSuggestionItemViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryName = product.Category?.Name ?? "Без категорії",
                Price = product.GetEffectiveDiscountPercent(userId) > 0 ? product.GetDiscountedPrice(userId) : product.Price,
                ImageUrl = product.MainImageUrl
            }).ToList()
        });
    }

    private static int LevenshteinDistance(string a, string b)
    {
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        var rows = a.Length + 1;
        var cols = b.Length + 1;
        var distance = new int[rows, cols];

        for (var i = 0; i < rows; i++) distance[i, 0] = i;
        for (var j = 0; j < cols; j++) distance[0, j] = j;

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < cols; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[rows - 1, cols - 1];
    }

    private static CatalogProductViewModel MapProduct(Marketplace.Domain.Entities.Product product, double averageRating = 0, int reviewCount = 0,string userId = null)
    {
        
        var discountPercent = product.GetEffectiveDiscountPercent(userId);
        var hasDiscount = discountPercent > 0;

        return new CatalogProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = hasDiscount ? product.GetDiscountedPrice(userId) : product.Price,
            Quantity = product.Quantity,
            Sku = product.Sku,
            Variants = product.Variants
                .OrderBy(v => v.Size)
                .Select(v => new CatalogProductVariantViewModel { Id = v.Id, Size = v.Size, Quantity = v.Quantity })
                .ToList(),
            CreatedAt = product.CreatedAt,
            MainImageUrl = product.MainImageUrl,
            CategoryName = product.Category?.Name ?? "Без категорії",
            StoreName = product.Business?.StoreName ?? "Marketplace",
            StoreLogoUrl = product.Business?.LogoUrl ?? string.Empty,
            StoreDescription = product.Business?.Description ?? string.Empty,
            AdditionalImageUrls = product.Images.Select(image => image.ImageUrl).ToList(),
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            OriginalPrice = hasDiscount ? product.Price : null,
            DiscountPercent = hasDiscount ? discountPercent : null,
            IsAgeRestricted = product.IsAdultOnly
        };
    }
}