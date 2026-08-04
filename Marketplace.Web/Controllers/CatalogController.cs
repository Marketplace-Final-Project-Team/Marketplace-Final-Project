using Marketplace.BLL.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers;

[AllowAnonymous]
public class CatalogController : Controller
{
    private readonly IProductService _productService;

    public CatalogController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var products = await _productService.GetCatalogAsync(search);
        ViewBag.Search = search;

        return View(products.Select(MapProduct).ToList());
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

        return View(model);
    }

    private static CatalogProductViewModel MapProduct(Marketplace.Domain.Entities.Product product)
    {
        return new CatalogProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            MainImageUrl = product.MainImageUrl,
            CategoryName = product.Category?.Name ?? "Без категорії",
            StoreName = product.Business?.StoreName ?? "Marketplace",
            StoreLogoUrl = product.Business?.LogoUrl ?? string.Empty,
            StoreDescription = product.Business?.Description ?? string.Empty,
            AdditionalImageUrls = product.Images.Select(image => image.ImageUrl).ToList()
        };
    }
}
