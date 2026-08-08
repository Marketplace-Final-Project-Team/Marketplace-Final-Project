using Marketplace.BLL.DTOs.Products;
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