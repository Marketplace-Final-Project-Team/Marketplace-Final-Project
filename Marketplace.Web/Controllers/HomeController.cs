using Marketplace.BLL.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Marketplace.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;

        public HomeController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetCatalogAsync();
            var featuredProducts = products
                .Take(3)
                .Select(product => new CatalogProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    MainImageUrl = product.MainImageUrl,
                    CategoryName = product.Category?.Name ?? "Без категорії",
                    StoreName = product.Business?.StoreName ?? "Marketplace"
                })
                .ToList();

            return View(featuredProducts);
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
}
