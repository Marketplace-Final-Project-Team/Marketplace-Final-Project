using System.Diagnostics;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Web.Infrastructure;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public HomeController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index([FromQuery] CatalogFilterViewModel filter)
        {
            const int pageSize = 12;
            filter.Page = filter.Page <= 0 ? 1 : filter.Page;

            var filterDto = new CatalogFilterDto
            {
                Search = filter.Search,
                CategoryId = filter.CategoryId,
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                InStockOnly = filter.InStockOnly,
                MinRating = filter.MinRating,
                Sort = filter.Sort,
                Page = filter.Page,
                PageSize = pageSize
            };

            var catalogPage = await _productService.GetCatalogPageAsync(filterDto);

            var featuredProducts = catalogPage.Items
                .Select(item =>
                {
                    var activeDiscount = item.Product.GetActiveDiscount();

                    return new CatalogProductViewModel
                    {
                        Id = item.Product.Id,
                        Name = item.Product.Name,
                        Description = item.Product.Description,
                        Price = activeDiscount != null ? item.Product.GetDiscountedPrice() : item.Product.Price,
                        Quantity = item.Product.Quantity,
                        MainImageUrl = item.Product.MainImageUrl,
                        CategoryName = item.Product.Category?.Name ?? "Без категорії",
                        StoreName = item.Product.Business?.StoreName ?? "Marketplace",
                        AverageRating = item.AverageRating,
                        ReviewCount = item.ReviewCount,
                        OriginalPrice = activeDiscount != null ? item.Product.Price : null,
                        DiscountPercent = activeDiscount?.Percent
                    };
                })
                .ToList();

            var rawCategories = await _categoryService.GetAllCategories();

            var colors = new[] { "blue", "violet", "orange", "green", "teal" };
            var icons = new[] { "📦", "💻", "👕", "🏠", "📚", "🎮" };

            var homeCategories = rawCategories
                .Select((c, index) => new HomeCategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    ColorClass = colors[index % colors.Length],
                    Icon = icons[index % icons.Length]
                })
                .ToList();

            var viewModel = new HomeViewModel
            {
                FeaturedProducts = featuredProducts,
                Categories = homeCategories,
                Search = filter.Search,
                CategoryId = filter.CategoryId,
                SortBy = filter.Sort.ToString(),
                RecentlyViewed = await GetRecentlyViewedAsync(),
                AverageRating = featuredProducts.Any() ? featuredProducts.Average(p => p.AverageRating) : null,
                ReviewCount = featuredProducts.Sum(p => p.ReviewCount)
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ProductListPartial", viewModel);
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> AutoComplete(string? term)
        {
            var suggestions = await _productService.GetSearchSuggestionsAsync(term);
            return Json(suggestions);
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