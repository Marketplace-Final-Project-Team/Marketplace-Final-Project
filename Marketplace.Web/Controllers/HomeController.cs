using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Web.Infrastructure;
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
                RecentlyViewed = await GetRecentlyViewedAsync(),
                AverageRating = totalReviews > 0
                    ? Math.Round(reviewedItems.Sum(item => item.AverageRating * item.ReviewCount) / totalReviews, 1)
                    : null,
                ReviewCount = totalReviews
            };

            return View(model);
        }

        private async Task<List<CatalogRelatedProductViewModel>> GetRecentlyViewedAsync()
        {
            Request.Cookies.TryGetValue(RecentlyViewedCookie.Name, out var raw);
            var ids = RecentlyViewedCookie.Parse(raw);

            var items = new List<CatalogRelatedProductViewModel>();
            foreach (var id in ids)
            {
                // Re-fetched from the database on every request — a product that was
                // deleted, banned, or sold out since the visit just won't come back here,
                // so this never shows stale name/price/stock data from an earlier view.
                var product = await _productService.GetCatalogProductAsync(id);
                if (product is null)
                {
                    continue;
                }

                items.Add(new CatalogRelatedProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    CategoryName = product.Category?.Name ?? "Без категорії",
                    MainImageUrl = product.MainImageUrl,
                    Price = product.Price,
                    Quantity = product.Quantity
                });
            }

            return items;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Support()
        {
            return View();
        }

        public IActionResult CookiePolicy()
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
