using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Marketplace.Web.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string[] CategoryColors = { "blue", "orange", "rose", "green", "violet", "yellow" };
        private static readonly string[] CategoryIcons = { "⌁", "⌂", "◇", "●", "≡", "✦" };

        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public HomeController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            // GetCatalogPageAsync already aggregates AverageRating/ReviewCount per product via SQL
            // (built for the catalog filters), so a single call here doubles as both "most popular
            // products" and the raw material for a real, weighted site-wide rating below — no need
            // for a per-product review loop.
            var catalogPage = await _productService.GetCatalogPageAsync(new CatalogFilterDto
            {
                Sort = CatalogSortOption.Popularity,
                Page = 1,
                PageSize = 1000
            });

            var featuredProducts = catalogPage.Items
                .Take(3)
                .Select(item => new CatalogProductViewModel
                {
                    Id = item.Product.Id,
                    Name = item.Product.Name,
                    Description = item.Product.Description,
                    Price = item.Product.Price,
                    Quantity = item.Product.Quantity,
                    MainImageUrl = item.Product.MainImageUrl,
                    CategoryName = item.Product.Category?.Name ?? "Без категорії",
                    StoreName = item.Product.Business?.StoreName ?? "Marketplace",
                    AverageRating = item.AverageRating,
                    ReviewCount = item.ReviewCount
                })
                .ToList();

            var reviewedItems = catalogPage.Items.Where(item => item.ReviewCount > 0).ToList();
            var totalReviews = reviewedItems.Sum(item => item.ReviewCount);

            var categories = await _categoryService.GetAllCategories();

            var model = new HomeViewModel
            {
                FeaturedProducts = featuredProducts,
                Categories = categories
                    .Select((category, index) => new HomeCategoryViewModel
                    {
                        Id = category.Id,
                        Name = category.Name,
                        ColorClass = CategoryColors[index % CategoryColors.Length],
                        Icon = CategoryIcons[index % CategoryIcons.Length]
                    })
                    .ToList(),
                AverageRating = totalReviews > 0
                    ? Math.Round(reviewedItems.Sum(item => item.AverageRating * item.ReviewCount) / totalReviews, 1)
                    : null,
                ReviewCount = totalReviews
            };

            return View(model);
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
