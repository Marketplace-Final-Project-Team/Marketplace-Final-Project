 using System.Security.Claims;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Services;
using Marketplace.Domain.Enums;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Common;
using Quartz;
using Quartz.Impl.Matchers;

namespace Marketplace.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ISystemSettingsService _settingsService;
        private readonly IActivityLogService _activityLog;
        private readonly IProductService _productService;
        private readonly IConfiguration _configuration;
        private readonly IMarketplaceBalanceService _marketplaceBalanceService;
        private readonly ICategoryService _categoryService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IDiscountService _discountService;

        public AdminController(
            IAdminService adminService,
            ISystemSettingsService settingsService,
            IActivityLogService activityLog,
            IProductService productService,
            IConfiguration configuration,
            IMarketplaceBalanceService marketplaceBalanceService,
            ICategoryService categoryService,
            ISchedulerFactory schedulerFactory,
            IDiscountService discountService)
        {
            _adminService = adminService;
            _settingsService = settingsService;
            _activityLog = activityLog;
            _productService = productService;
            _configuration = configuration;
            _marketplaceBalanceService = marketplaceBalanceService;
            _categoryService = categoryService;
            _schedulerFactory = schedulerFactory;
            _discountService = discountService;
        }

        private string? CurrentAdminId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index()
        {
            var users = _adminService.GetUsers();
            var products = await _adminService.GetProductsAsync();
            var businesses = await _adminService.GetBusinessesAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                BannedUsers = users.Count(u => u.Status == UserStatus.Banned),
                TotalProducts = products.Count,
                PendingProducts = products.Count(p => p.Status == ProductStatus.Pending),
                BannedProducts = products.Count(p => p.Status == ProductStatus.Banned),
                TotalBusinesses = businesses.Count,
                PendingBusinesses = businesses.Count(b => b.Status == BusinessStatus.Pending),
                MarketplaceBalance = await _marketplaceBalanceService.GetBalanceAsync(),
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Users([FromQuery] UserAdminFilterViewModel filter)
        {
            var result = await _adminService.GetFilteredUsersAsync(new UserAdminQueryDto
            {
                Search = filter.Search,
                Status = filter.Status,
                SortDescending = filter.SortDescending,
                Pagination = new PaginationParams
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize
                }
            });

            return View(new AdminUsersPageViewModel
            {
                Filter = filter,
                Result = new PagedResult<UserItemAdminViewModel>
                {
                    Items = result.Items.Select(u => new UserItemAdminViewModel
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Status = u.Status,
                    }).ToList(),
                    TotalItems = result.TotalItems,
                    Page = result.Page,
                    TotalPages = result.TotalPages,
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> Products([FromQuery] ProductAdminFilterViewModel filter)
        {
            var result = await _adminService.GetFilteredProductsAsync(new ProductAdminQueryDto
            {
                CategoryId = filter.CategoryId,
                Search = filter.Search,
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                InStockOnly = filter.InStockOnly,
                SortOption = filter.SortOption,
                Pagination = new PaginationParams
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize
                }
            });

            return View(new AdminProductsPageViewModel
            {
                Filter = filter,
                Categories = await _categoryService.GetAllCategories(),
                Result = new PagedResult<ProductItemAdminViewModel>
                {
                    Items = result.Items.Select(p => new ProductItemAdminViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ImageUrl = p.ImageUrl,
                        Price = p.Price,
                        Business = p.Business,
                        Status = p.Status,
                    }).ToList(),
                    TotalItems = result.TotalItems,
                    Page = result.Page,
                    TotalPages = result.TotalPages,
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> Businesses([FromQuery] BusinessAdminFilterViewModel filter)
        {
            var result = await _adminService.GetFilteredBusinessesAsync(new BusinessAdminQueryParams
            {
                Search = filter.Search,
                Status = filter.Status,
                SortOption = filter.SortOption,
                Pagination = new PaginationParams
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize
                }
            });

            var items = new List<BusinessItemAdminViewModel>();
            foreach (var b in result.Items)
            {
                var discount = await _discountService.GetBusinessDiscount(b.Id);
                items.Add(new BusinessItemAdminViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    LogoUrl = b.LogoUrl,
                    Owner = b.Owner,
                    Status = b.Status,
                    CommissionPercent = b.CommissionPercent,
                    HasActiveDiscount = discount.IsActive,
                    DiscountPercent = discount.DiscountPercent,
                    DiscountEndDate = discount.EndDate,
                });
            }

            return View(new AdminBusinessesPageViewModel
            {
                Filter = filter,
                Result = new PagedResult<BusinessItemAdminViewModel>
                {
                    Items = items,
                    TotalItems = result.TotalItems,
                    Page = result.Page,
                    TotalPages = result.TotalPages,
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserStatus(string userId, UserStatus status)
        {
            var result = await _adminService.ChangeUserStatus(userId, status);
            if (result.Succeeded)
            {
                var action = status == UserStatus.Banned ? "Banned" : "Unbanned";
                await _activityLog.LogUserActionAsync(userId, action,
                    status == UserStatus.Banned ? "Обліковий запис заблоковано адміністратором." : "Обліковий запис розблоковано адміністратором.",
                    actorUserId: CurrentAdminId);
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeProductStatus(Guid productId, ProductStatus status)
        {
            var product = await _productService.GetByIdAsync(productId);
            var result = await _adminService.ChangeProductStatus(productId, status);
            if (result.Succeeded && product is not null)
            {
                await _activityLog.LogBusinessActionAsync(product.BusinessId, "ProductStatusChanged",
                    $"Статус товару «{product.Name}» змінено адміністратором на «{status}».",
                    actorUserId: CurrentAdminId, relatedEntityId: productId);
            }
            return RedirectToAction("Products");
        }


        [HttpPost]
        public async Task<IActionResult> ChangeBusinessStatus(Guid businessId, BusinessStatus status)
        {
            var result = await _adminService.ChangeBusinessStatus(businessId, status);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "BusinessStatusChanged",
                    $"Статус бізнесу змінено адміністратором на «{status}».",
                    actorUserId: CurrentAdminId);
            }
            return RedirectToAction("Businesses");
        }

        [HttpGet]
        public async Task<IActionResult> Integrations()
        {
            return View(await BuildIntegrationsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateIntegrationKey(string category, string key, string value, bool isSecret)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                TempData["Error"] = "Введіть значення перед збереженням.";
                return RedirectToAction("Integrations");
            }

            await _settingsService.SetAsync(category, key, value, isSecret, CurrentAdminId);
            TempData["Success"] = $"Ключ «{key}» ({category}) збережено.";
            return RedirectToAction("Integrations");
        }

        [HttpGet]
        public async Task<IActionResult> Jobs()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals("moderation"));
            var scheduledJobs = new List<ScheduledModerationJobViewModel>();

            foreach (var jobKey in jobKeys)
            {
                if (!Guid.TryParse(jobKey.Name.Replace("moderation-job-", string.Empty), out var productId))
                {
                    continue;
                }

                var product = await _productService.GetByIdAsync(productId);
                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                var nextFireTime = triggers
                    .Select(t => t.GetNextFireTimeUtc())
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value.UtcDateTime)
                    .OrderBy(t => t)
                    .Cast<DateTime?>()
                    .FirstOrDefault();

                scheduledJobs.Add(new ScheduledModerationJobViewModel
                {
                    ProductId = productId,
                    ProductName = product?.Name ?? "(товар видалено)",
                    NextRunAtUtc = nextFireTime
                });
            }

            var recentDecisions = (await _activityLog.GetRecentBusinessActionsAsync())
                .Where(l => l.Action == ProductModerationJob.ActivityLogAction)
                .Take(30)
                .Select(l => new BusinessActivityLogItemViewModel
                {
                    BusinessName = l.BusinessName,
                    ActorUserId = l.ActorUserId,
                    Action = l.Action,
                    Description = l.Description,
                    CreatedAt = l.CreatedAt
                })
                .ToList();

            return View(new JobsPageViewModel
            {
                IsSchedulerRunning = scheduler.IsStarted && !scheduler.InStandbyMode && !scheduler.IsShutdown,
                ScheduledJobs = scheduledJobs.OrderBy(j => j.NextRunAtUtc).ToList(),
                RecentDecisions = recentDecisions
            });
        }

        [HttpGet]
        public async Task<IActionResult> BusinessLimits()
        {
            var defaults = new BusinessLimitsViewModel();
            return View(new BusinessLimitsViewModel
            {
                MaxProductsPerBusiness = await _settingsService.GetIntAsync("BusinessRules", "MaxProductsPerBusiness", defaults.MaxProductsPerBusiness),
                MaxDiscountPercent = await _settingsService.GetIntAsync("BusinessRules", "MaxDiscountPercent", defaults.MaxDiscountPercent),
                MaxPromoCodePercent = await _settingsService.GetIntAsync("BusinessRules", "MaxPromoCodePercent", defaults.MaxPromoCodePercent),
                MaxActivePromoCodesPerBusiness = await _settingsService.GetIntAsync("BusinessRules", "MaxActivePromoCodesPerBusiness", defaults.MaxActivePromoCodesPerBusiness),
                PendingOrderExpiryHours = await _settingsService.GetIntAsync("BusinessRules", "PendingOrderExpiryHours", defaults.PendingOrderExpiryHours),
                LowStockThreshold = await _settingsService.GetIntAsync("BusinessRules", "LowStockThreshold", defaults.LowStockThreshold),
            });
        }

        [HttpPost]
        public async Task<IActionResult> BusinessLimits(BusinessLimitsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Перевірте введені значення - деякі з них поза допустимими межами.";
                return View(model);
            }

            await _settingsService.SetAsync("BusinessRules", "MaxProductsPerBusiness", model.MaxProductsPerBusiness.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "MaxDiscountPercent", model.MaxDiscountPercent.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "MaxPromoCodePercent", model.MaxPromoCodePercent.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "MaxActivePromoCodesPerBusiness", model.MaxActivePromoCodesPerBusiness.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "PendingOrderExpiryHours", model.PendingOrderExpiryHours.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "LowStockThreshold", model.LowStockThreshold.ToString(), false, CurrentAdminId);

            TempData["Success"] = "Ліміти бізнесу збережено.";
            return RedirectToAction("BusinessLimits");
        }

        [HttpGet]
        public async Task<IActionResult> MarketplaceBalance()
        {
            return View(new MarketplaceBalanceViewModel
            {
                Balance = await _marketplaceBalanceService.GetBalanceAsync(),
            });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeBusinessCommission(Guid businessId, decimal commissionPercent)
        {
            var result = await _adminService.ChangeBusinessComission(businessId, commissionPercent);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "CommissionChanged",
                    $"Комісію бізнесу змінено адміністратором на {commissionPercent}%.",
                    actorUserId: CurrentAdminId);
            }
            else
            {
                TempData["Error"] = result.Message;
            }
            return RedirectToAction("Businesses");
        }

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _categoryService.GetAllCategories();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Назва категорії не може бути порожньою.";
                return RedirectToAction("Categories");
            }

            var result = await _categoryService.CreateCategory(name.Trim());
            if (result.Succeeded)
            {
                TempData["Success"] = $"Категорію «{name}» створено.";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory(Guid categoryId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Назва категорії не може бути порожньою.";
                return RedirectToAction("Categories");
            }

            var result = await _categoryService.UpdateCategory(name.Trim(), categoryId);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Категорію оновлено." : result.Message;

            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(Guid categoryId)
        {
            var result = await _categoryService.DeleteCategory(categoryId);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Категорію видалено." : result.Message;

            return RedirectToAction("Categories");
        }

        [HttpGet]
        public async Task<IActionResult> UserActivityLog()
        {
            var logs = await _activityLog.GetRecentUserActionsAsync();
            return View(logs.Select(l => new UserActivityLogItemViewModel
            {
                UserEmail = l.UserEmail,
                ActorUserId = l.ActorUserId,
                Action = l.Action,
                Description = l.Description,
                CreatedAt = l.CreatedAt
            }).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> BusinessActivityLog()
        {
            var logs = await _activityLog.GetRecentBusinessActionsAsync();
            return View(logs.Select(l => new BusinessActivityLogItemViewModel
            {
                BusinessName = l.BusinessName,
                ActorUserId = l.ActorUserId,
                Action = l.Action,
                Description = l.Description,
                CreatedAt = l.CreatedAt
            }).ToList());
        }

        private async Task<List<IntegrationSettingViewModel>> BuildIntegrationsAsync()
        {
            var definitions = new (string Category, string DisplayName, string Description, string ConfigSection, (string Key, string Label, bool IsSecret)[] Keys)[]
            {
                ("LiqPay", "LiqPay", "Приймання онлайн-оплат за замовлення.", "LiqPay", new[]
                {
                    ("PublicKey", "Public Key", false),
                    ("PrivateKey", "Private Key", true),
                    ("ServerUrl", "Server URL (callback)", false),
                }),
                ("NovaPoshta", "Нова пошта", "Пошук міст і відділень для доставки.", "DeliveryApi", new[]
                {
                    ("NovaPoshtaApiKey", "API Key", true),
                    ("NovaPoshtaBaseUrl", "Base URL", false),
                }),
                ("Cloudinary", "Cloudinary", "Зберігання зображень товарів і магазинів.", "Cloudinary", new[]
                {
                    ("CloudName", "Cloud Name", false),
                    ("ApiKey", "API Key", true),
                    ("ApiSecret", "API Secret", true),
                }),
                ("Email", "Email (SMTP)", "Підтвердження реєстрації, скидання пароля, сповіщення.", "EmailSettings", new[]
                {
                    ("Host", "SMTP Host", false),
                    ("Port", "Port", false),
                    ("Username", "Username", false),
                    ("Password", "Password", true),
                    ("FromEmail", "From Email", false),
                    ("FromName", "From Name", false),
                }),
                ("GoogleAI", "Google AI (Gemini)", "Автоматична модерація товарів (текст і зображення).", "GoogleAI", new[]
                {
                    ("ApiKey", "API Key", true),
                    ("Model", "Model", false),
                }),
            };

            var result = new List<IntegrationSettingViewModel>();

            foreach (var def in definitions)
            {
                var saved = await _settingsService.GetByCategoryAsync(def.Category);
                var keys = new List<IntegrationKeyViewModel>();

                foreach (var (key, label, isSecret) in def.Keys)
                {
                    var savedRow = saved.FirstOrDefault(s => s.Key == key);
                    var value = savedRow?.Value ?? _configuration[$"{def.ConfigSection}:{key}"] ?? string.Empty;

                    keys.Add(new IntegrationKeyViewModel
                    {
                        Key = key,
                        Label = label,
                        IsSecret = isSecret,
                        IsOverridden = savedRow is not null,
                        MaskedValue = Mask(value, isSecret)
                    });
                }

                result.Add(new IntegrationSettingViewModel
                {
                    Category = def.Category,
                    DisplayName = def.DisplayName,
                    Description = def.Description,
                    IsConfigured = keys.Any(k => !string.IsNullOrWhiteSpace(k.MaskedValue) && k.MaskedValue != "-"),
                    Keys = keys
                });
            }

            return result;
        }

        private static string Mask(string value, bool isSecret)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "-";
            }

            if (!isSecret)
            {
                return value;
            }

            return value.Length <= 4
                ? new string('*', 8)
                : new string('*', 8) + value[^4..];
        }


        [HttpGet]
        public async Task<IActionResult> GetTotalCommissionsAsync() 
        {
            var totalCommission = await _adminService.GetTotalCommissionAsync();

            return View(totalCommission);
        }

        [HttpPost]
        public async Task<IActionResult> GetCommissionsStatsByPeriodAsync(CommissionStatsQueryViewModel query) 
        {
            var res = await _adminService.GetCommissionStatsByPeriodAsync(query.StartDate, query.EndDate);

            if(!res.Succeeded) 
            {
                return NotFound();
            }

            return View(new CommissionStatsAdminViewModel
            {
                Daily = res.Data!.Daily,
                ByBusiness = res.Data!.ByBusiness,
                TotalCommissionByPeriod = res.Data!.TotalCommissionByPeriod,
            });
        }
    }
}