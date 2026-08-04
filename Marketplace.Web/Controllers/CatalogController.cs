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
        return product is null
            ? NotFound()
            : View(MapProduct(product));
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
            AdditionalImageUrls = product.Images.Select(image => image.ImageUrl).ToList()
        };
    }
}
