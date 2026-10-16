using System.Diagnostics;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.Web.Infrastructure;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ISupportTicketService _supportTicketService;
        private readonly UserManager<User> _userManager;

        public HomeController(
          IProductService productService,
          ICategoryService categoryService,
          ISupportTicketService supportTicketService,
          UserManager<User> userManager)
            {
                _productService = productService;
                _categoryService = categoryService;
                _supportTicketService = supportTicketService;
                _userManager = userManager;
            }

        public async Task<IActionResult> Index([FromQuery] CatalogueFilterViewModel filter)
        {
            var userId = _userManager.GetUserId(User);
            filter.Page = filter.Page <= 0 ? 1 : filter.Page;

            var sort = Request.Query.ContainsKey("Sort") ? filter.Sort : CatalogueSortOption.Popularity;

            var rawCategories = await _categoryService.GetAllCategories();

            var colors = new[] { "blue", "violet", "orange", "green", "teal" };

            var homeCategories = rawCategories
                .Select((c, index) => new HomeCategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    ColorClass = colors[index % colors.Length],
                    Icon = CategoryIcon(c.Name),
                    ImageUrl = c.ImageUrl,
                    ProductCount = c.ActiveProductCount
                })
                .ToList();

            const int productsPerCategorySection = 8;
            const int maxCategorySections = 6;

            var categorySections = new List<HomeCategoryProductsViewModel>();
            foreach (var category in rawCategories.Where(c => c.ActiveProductCount > 0)
                         .OrderByDescending(c => c.ActiveProductCount)
                         .Take(maxCategorySections))
            {
                var categoryPage = await _productService.GetCataloguePageAsync(new CatalogueFilterDto
                {
                    CategoryId = category.Id,
                    Sort = CatalogueSortOption.Popularity,
                    Page = 1,
                    PageSize = productsPerCategorySection
                });

                if (categoryPage.Items.Count == 0)
                {
                    continue;
                }

                categorySections.Add(new HomeCategoryProductsViewModel
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Products = categoryPage.Items.Select(item => MapCatalogProduct(item, userId)).ToList()
                });
            }

            var viewModel = new HomeViewModel
            {
                Categories = homeCategories,
                CategorySections = categorySections,
                Search = filter.Search,
                CategoryId = filter.CategoryId,
                SortBy = sort.ToString(),
                RecentlyViewed = (await RecentlyViewedCookie.LoadAsync(_productService, Request.Cookies, userId)).Take(3).ToList()
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

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Support()
        {
            var userId = _userManager.GetUserId(User);

            var activeTicket = !string.IsNullOrEmpty(userId)
                ? await _supportTicketService.GetActiveTicketForUserAsync(userId)
                : null;

            return View(new SupportPageViewModel
            {
                IsAuthenticated = !string.IsNullOrEmpty(userId),
                ActiveTicket = activeTicket
            });
        }


        public IActionResult CookiePolicy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult ReturnsPolicy()
        {
            return View();
        }

        public IActionResult ShippingPolicy()
        {
            return View();
        }

        public IActionResult SellerRules()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static CatalogueProductViewModel MapCatalogProduct(CataloguePageItemDto item, string? userId)
        {
            var discountPercent = item.Product.GetEffectiveDiscountPercent(userId);
            var hasDiscount = discountPercent > 0;

            return new CatalogueProductViewModel
            {
                Id = item.Product.Id,
                Name = item.Product.Name,
                Description = item.Product.Description,
                Price = hasDiscount ? item.Product.GetDiscountedPrice(userId) : item.Product.Price,
                Quantity = item.Product.Quantity,
                MainImageUrl = item.Product.MainImageUrl,
                CategoryName = item.Product.Category?.Name ?? "Без категорії",
                StoreName = item.Product.Business?.StoreName ?? "Marketplace",
                AverageRating = item.AverageRating,
                ReviewCount = item.ReviewCount,
                OriginalPrice = hasDiscount ? item.Product.Price : null,
                DiscountPercent = hasDiscount ? discountPercent : null,
                IsAgeRestricted = item.Product.IsAdultOnly
            };
        }

        // Keyword-matched rather than a fixed per-position list, so a category keeps a
        // sensible icon regardless of the order it comes back in — and admin-created
        // categories that don't match any keyword still get a reasonable default.
        private static string CategoryIcon(string categoryName)
        {
            var name = categoryName.ToLowerInvariant();

            if (name.Contains("електрон") || name.Contains("технік") || name.Contains("гаджет")) return "💻";
            if (name.Contains("спорт") || name.Contains("відпочин") || name.Contains("туризм")) return "🏀";
            if (name.Contains("дом") || name.Contains("кухня") || name.Contains("побут")) return "🏠";
            if (name.Contains("одяг") || name.Contains("взутт") || name.Contains("мод")) return "👕";
            if (name.Contains("книг") || name.Contains("канцеляр")) return "📚";
            if (name.Contains("іграшк") || name.Contains("дит")) return "🧸";
            if (name.Contains("краса") || name.Contains("косметик") || name.Contains("здоров'я")) return "💄";
            if (name.Contains("авто") || name.Contains("транспорт")) return "🚗";
            if (name.Contains("тварин") || name.Contains("зоо")) return "🐾";
            if (name.Contains("сад") || name.Contains("город") || name.Contains("рослин")) return "🌱";

            return "🛍";
        }
    }
}