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
using Microsoft.Extensions.Configuration;

namespace Marketplace.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ISystemSettingsService _settingsService;
        private readonly IActivityLogService _activityLog;
        private readonly IProductService _productService;
        private readonly IBusinessService _businessService;
        private readonly IMarketplaceBalanceService _marketplaceBalanceService;
        private readonly ICategoryService _categoryService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IDiscountService _discountService;
        private readonly IConfiguration _configuration;

        public AdminController(
            IAdminService adminService,
            ISystemSettingsService settingsService,
            IActivityLogService activityLog,
            IProductService productService,
            IMarketplaceBalanceService marketplaceBalanceService,
            ICategoryService categoryService,
            ISchedulerFactory schedulerFactory,
            IDiscountService discountService,
            IConfiguration configuration,
            IBusinessService businessService)
        {
            _adminService = adminService;
            _settingsService = settingsService;
            _activityLog = activityLog;
            _productService = productService;
            _marketplaceBalanceService = marketplaceBalanceService;
            _categoryService = categoryService;
            _schedulerFactory = schedulerFactory;
            _discountService = discountService;
            _configuration = configuration;
            _businessService = businessService;
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
                SearchBusiness = filter.SearchBusiness,
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                InStockOnly = filter.InStockOnly,
                Status = filter.Status,
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
        public async Task<IActionResult> ProductDetailsPartial(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            return PartialView("_ProductDetailsModal", new ProductAdminDetailsViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                MainImageUrl = product.MainImageUrl,
                AdditionalImageUrls = product.Images.Select(i => i.ImageUrl).ToList(),
                Status = product.Status,
                ModerationReason = product.ModerationReason,
                CategoryName = product.Category?.Name ?? "Без категорії",
                BusinessId = product.BusinessId,
                BusinessName = product.Business?.StoreName ?? "Без назви",
                CreatedAt = product.CreatedAt
            });
        }

        [HttpGet]
        public async Task<IActionResult> Businesses([FromQuery] BusinessAdminFilterViewModel filter)
        {
            var result = await _adminService.GetFilteredBusinessesAsync(new BusinessAdminQueryParams
            {
                Search = filter.Search,
                SearchEmail = filter.SearchEmail,
                Status = filter.Status,
                MinCommission = filter.MinCommission,
                MaxCommission = filter.MaxCommission,
                SortOption = filter.SortOption,
                Pagination = new PaginationParams
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize
                }
            });

            var items = result.Items.Select(b => new BusinessItemAdminViewModel
            {
                Id = b.Id,
                Name = b.Name,
                LogoUrl = b.LogoUrl,
                Owner = b.Owner,
                Status = b.Status,
                CommissionPercent = b.CommissionPercent,
            }).ToList();

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

        // BusinessLimits is backed by the real SystemSettings table (see
        // ISystemSettingsService) — saved values persist for real and drive live
        // behaviour (the BusinessRules caps enforced in DiscountService/PromoCodeService/
        // ProductsController plus the PendingOrderExpiryJob and the catalog low-stock badge).

        [HttpGet]
        public async Task<IActionResult> Jobs()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals("moderation"));
            var scheduledJobs = new List<ScheduledModerationJobViewModel>();

            foreach (var jobKey in jobKeys)
            {
                string? entityType = null;
                Guid entityId;

                if (jobKey.Name.StartsWith("product-moderation-job-") &&
                    Guid.TryParse(jobKey.Name.Replace("product-moderation-job-", string.Empty), out var productId))
                {
                    entityType = "Product";
                    entityId = productId;
                }
                else if (jobKey.Name.StartsWith("business-moderation-job-") &&
                    Guid.TryParse(jobKey.Name.Replace("business-moderation-job-", string.Empty), out var businessId))
                {
                    entityType = "Business";
                    entityId = businessId;
                }
                else
                {
                    continue;
                }

                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                var nextFireTime = triggers
                    .Select(t => t.GetNextFireTimeUtc())
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value.UtcDateTime)
                    .OrderBy(t => t)
                    .Cast<DateTime?>()
                    .FirstOrDefault();

                string name;
                if (entityType == "Product")
                {
                    var product = await _productService.GetByIdAsync(entityId);
                    name = product?.Name ?? "(товар видалено)";
                }
                else
                {
                    var business = await _businessService.GetByIdAsync(entityId);
                    name = business?.StoreName ?? "(бізнес видалено)";
                }

                scheduledJobs.Add(new ScheduledModerationJobViewModel
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    ProductName = name,
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

        [HttpGet]
        public async Task<IActionResult> Statistics([FromQuery] AdminStatisticsFilterViewModel filter)
        {
            var overview = await _adminService.GetStatisticsAsync(filter.StartDate, filter.EndDate);

            return View(new AdminStatisticsPageViewModel
            {
                Filter = filter,
                Overview = overview
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

            if (!res.Succeeded)
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

        [HttpPost]
        public async Task<IActionResult> PauseScheduler()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            if (scheduler.InStandbyMode)
            {
                TempData["Error"] = "Планувальник вже призупинено.";
                return RedirectToAction("Jobs");
            }

            await scheduler.Standby();

            await _activityLog.LogUserActionAsync(CurrentAdminId!, "SchedulerPaused",
                "Планувальник задач модерації призупинено адміністратором.",
                actorUserId: CurrentAdminId);

            TempData["Success"] = "Планувальник призупинено. Нові задачі не запускатимуться до відновлення.";
            return RedirectToAction("Jobs");
        }

        [HttpPost]
        public async Task<IActionResult> ResumeScheduler()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            if (!scheduler.InStandbyMode)
            {
                TempData["Error"] = "Планувальник і так активний.";
                return RedirectToAction("Jobs");
            }

            await scheduler.Start();

            await _activityLog.LogUserActionAsync(CurrentAdminId!, "SchedulerResumed",
                "Планувальник задач модерації відновлено адміністратором.",
                actorUserId: CurrentAdminId);

            TempData["Success"] = "Планувальник відновлено.";
            return RedirectToAction("Jobs");
        }

        [HttpPost]
        public async Task<IActionResult> RunModerationJobNow(Guid entityId, string entityType)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobName = entityType == "Business"
                ? $"business-moderation-job-{entityId}"
                : $"product-moderation-job-{entityId}";
            var jobKey = new JobKey(jobName, "moderation");

            if (!await scheduler.CheckExists(jobKey))
            {
                TempData["Error"] = "Задачу не знайдено в черзі — можливо, вона вже виконана.";
                return RedirectToAction("Jobs");
            }

            await scheduler.TriggerJob(jobKey);

            await _activityLog.LogUserActionAsync(CurrentAdminId!, "ModerationJobTriggeredManually",
                $"Модерацію ({entityType}) {entityId} запущено вручну адміністратором.",
                actorUserId: CurrentAdminId);

            TempData["Success"] = "Задачу запущено негайно.";
            return RedirectToAction("Jobs");
        }

        [HttpGet]
        public async Task<IActionResult> BusinessDetails(Guid id)
        {
            var details = await _adminService.GetBusinessDetailsAsync(id);
            if (details == null)
            {
                return NotFound();
            }

            var discount = await _discountService.GetBusinessDiscount(id);

            var model = new AdminBusinessDetailsViewModel
            {
                Id = details.Id,
                StoreName = details.StoreName,
                Description = details.Description,
                LogoUrl = details.LogoUrl,
                BannerUrl = details.BannerUrl,
                Status = details.Status,
                CommissionPercent = details.CommissionPercent,
                CreatedAt = details.CreatedAt,
                AdultStatus = details.AdultStatus,
                AdultRejectionReason = details.AdultRejectionReason,
                OwnerId = details.OwnerId,
                OwnerEmail = details.OwnerEmail,
                TotalProductCount = details.TotalProductCount,
                ActiveProductCount = details.ActiveProductCount,
                PendingProductCount = details.PendingProductCount,
                BannedProductCount = details.BannedProductCount,
                HasActiveDiscount = discount.IsActive,
                DiscountPercent = discount.DiscountPercent,
                DiscountEndDate = discount.EndDate
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveAdultStatus(Guid businessId)
        {
            var result = await _adminService.ApproveAdultStatusAsync(businessId);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction("BusinessDetails", new { id = businessId });
        }

        [HttpPost]
        public async Task<IActionResult> RejectAdultStatus(Guid businessId, string reason)
        {
            var result = await _adminService.RejectAdultStatusAsync(businessId, reason);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction("BusinessDetails", new { id = businessId });
        }
    }
}