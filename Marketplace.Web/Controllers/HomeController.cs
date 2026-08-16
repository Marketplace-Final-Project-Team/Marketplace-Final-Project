using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;

namespace Marketplace.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly IGenericRepository<Category> _categoryRepository;

    public HomeController(
        IProductService productService,
        IGenericRepository<Category> categoryRepository)
    {
        _productService = productService;
        _categoryRepository = categoryRepository;
    }

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

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_ProductListPartial", model);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AutoComplete(string? term)
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}