using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Controllers;

[AllowAnonymous]
public class CatalogController : Controller
{
    private readonly IProductService _productService;
    private readonly IGenericRepository<Category> _categoryRepository;

    public CatalogController(
        IProductService productService,
        IGenericRepository<Category> categoryRepository)
    {
        _productService = productService;
        _categoryRepository = categoryRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, Guid? categoryId, string? sortBy, int page = 1)
    {
        var query = new ProductCatalogQueryDto
        {
            Search = search,
            CategoryId = categoryId,
            SortBy = sortBy,
            Page = page <= 0 ? 1 : page,
            PageSize = 12
        };

        var result = await _productService.GetCatalogAsync(query);
        var categories = await _categoryRepository.GetAllAsync();

        var model = new CatalogIndexViewModel
        {
            Products = result.Items.Select(p => p.ToCatalogProductViewModel()).ToList(),
            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == categoryId
            }),
            Search = search,
            CategoryId = categoryId,
            SortBy = sortBy,
            Page = result.Page,
            TotalPages = result.TotalPages
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SearchSuggestions(string? term)
    {
        var suggestions = await _productService.GetSearchSuggestionsAsync(term);

        var result = suggestions.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            categoryName = string.IsNullOrWhiteSpace(s.CategoryName) ? "Без категорії" : s.CategoryName,
            mainImageUrl = s.MainImageUrl,
            price = s.Price.ToString("N2") + " ₴",
            hasActiveDiscount = s.HasActiveDiscount,
            discountedPrice = s.DiscountedPrice.HasValue ? s.DiscountedPrice.Value.ToString("N2") + " ₴" : null,
            discountPercentage = s.DiscountPercentage
        });

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var product = await _productService.GetCatalogProductAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var model = product.ToCatalogProductViewModel();

        var catalog = await _productService.GetCatalogAsync(new ProductCatalogQueryDto
        {
            CategoryId = product.CategoryId,
            PageSize = 5
        });

        model.EstimatedDeliveryStart = DateTime.Today.AddDays(2);
        model.EstimatedDeliveryEnd = DateTime.Today.AddDays(4);

        model.RelatedProducts = catalog.Items
            .Where(candidate => candidate.Id != product.Id)
            .Take(4)
            .Select(candidate => new CatalogRelatedProductViewModel
            {
                Id = candidate.Id,
                Name = candidate.Name,
                CategoryName = product.Category?.Name ?? "Без категорії",
                MainImageUrl = candidate.MainImageUrl,
                Price = candidate.Price,
                Quantity = candidate.Quantity,
            })
            .ToList();

        return View(model);
    }
}