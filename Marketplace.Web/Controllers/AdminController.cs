using Marketplace.Web.Infrastructure;
using System.Security.Claims;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Enums;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Common;
using Quartz;
using Quartz.Impl.Matchers;
using Marketplace.BLL.Jobs;

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
        private readonly IOrderService _orderService;
        private readonly IPostSaleService _postSaleService;
        private readonly IRefundChatService _refundChatService;
        private readonly ISupportTicketService _supportTicketService;
        private readonly IWithdrawalService _withdrawalService;

        public AdminController(
            IAdminService adminService,
            ISystemSettingsService settingsService,
            IActivityLogService activityLog,
            IProductService productService,
            IMarketplaceBalanceService marketplaceBalanceService,
            ICategoryService categoryService,
            ISchedulerFactory schedulerFactory,
            IBusinessService businessService,
            IDiscountService discountService,
            IOrderService orderService,
            IPostSaleService postSaleService,
            IRefundChatService refundChatService,
            ISupportTicketService supportTicketService,
            IWithdrawalService withdrawalService)
        {
            _adminService = adminService;
            _settingsService = settingsService;
            _activityLog = activityLog;
            _productService = productService;
            _marketplaceBalanceService = marketplaceBalanceService;
            _categoryService = categoryService;
            _schedulerFactory = schedulerFactory;
            _businessService = businessService;
            _discountService = discountService;
            _orderService = orderService;
            _postSaleService = postSaleService;
            _refundChatService = refundChatService;
            _supportTicketService = supportTicketService;
            _withdrawalService = withdrawalService;
        }

        private string? CurrentAdminId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index()
        {
            var users = _adminService.GetUsers();
            var products = await _adminService.GetProductsAsync();
            var businesses = await _adminService.GetBusinessesAsync();
            var adultRequests = await _adminService.GetPendingAdultRequestsAsync();
            var pendingWithdrawals = await _withdrawalService.GetAllWithdrawalsAsync(WithdrawalStatus.Pending);

            var model = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                BannedUsers = users.Count(u => u.Status == UserStatus.Banned),
                TotalProducts = products.Count,
                PendingProducts = products.Count(p => p.Status == ProductStatus.Pending),
                BannedProducts = products.Count(p => p.Status == ProductStatus.Banned),
                TotalBusinesses = businesses.Count,
                PendingBusinesses = businesses.Count(b => b.Status == BusinessStatus.Pending),
                PendingAdultRequests = adultRequests.Count,
                AdultRequestsQueue = adultRequests.ToList(),
                PendingWithdrawals = pendingWithdrawals.Count,
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
        public async Task<IActionResult> ChangeBusinessStatus(Guid businessId, BusinessStatus status, string? returnUrl = null)
        {
            var result = await _adminService.ChangeBusinessStatus(businessId, status);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "BusinessStatusChanged",
                    $"Статус бізнесу змінено адміністратором на «{status}».",
                    actorUserId: CurrentAdminId);
            }
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Businesses");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveAdultStatus(Guid businessId, string? returnUrl = null)
        {
            var result = await _adminService.ApproveAdultStatusAsync(businessId);
            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "AdultStatusApproved",
                    "Статус 18+ підтверджено адміністратором.", actorUserId: CurrentAdminId);
            }
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("BusinessDetails", new { businessId });
        }

        [HttpPost]
        public async Task<IActionResult> RejectAdultStatus(Guid businessId, string reason, string? returnUrl = null)
        {
            var result = await _adminService.RejectAdultStatusAsync(businessId, reason);
            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "AdultStatusRejected",
                    $"Заявку на статус 18+ відхилено адміністратором: {reason}", actorUserId: CurrentAdminId);
            }
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("BusinessDetails", new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> BusinessDetails(Guid businessId)
        {
            var details = await _adminService.GetBusinessDetailsAsync(businessId);
            if (details is null)
            {
                return NotFound();
            }

            var orderSummary = await _orderService.GetBusinessOrderSummaryAsync(businessId);
            var businessDiscount = await _discountService.GetBusinessDiscountAsync(businessId);
            var categoryDiscounts = await _discountService.GetActiveCategoryDiscountsForBusinessAsync(businessId);

            return View(new AdminBusinessDetailsViewModel
            {
                Id = details.Id,
                StoreName = details.StoreName,
                Description = details.Description,
                LogoUrl = details.LogoUrl,
                BannerUrl = details.BannerUrl,
                Status = details.Status,
                CommissionPercent = details.CommissionPercent,
                CreatedAt = details.CreatedAt,
                OwnerEmail = details.OwnerEmail,
                AdultStatus = details.AdultStatus,
                AdultRejectionReason = details.AdultRejectionReason,
                TotalProductCount = details.TotalProductCount,
                ActiveProductCount = details.ActiveProductCount,
                PendingProductCount = details.PendingProductCount,
                BannedProductCount = details.BannedProductCount,
                TotalOrders = orderSummary.TotalOrders,
                SoldItems = orderSummary.SoldItems,
                Revenue = orderSummary.Revenue,
                BusinessDiscount = businessDiscount,
                CategoryDiscounts = categoryDiscounts
            });
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(string userId)
        {
            var dto = await _adminService.GetUserDetailsAsync(userId);
            if (dto == null)
            {
                return NotFound();
            }

            var model = new UserAdminDetailsViewModel
            {
                Id = dto.Id,
                UserName = dto.UserName,
                Email = dto.Email,
                AvatarUrl = dto.AvatarUrl,
                Status = dto.Status,
                CreatedAt = dto.CreatedAt
            };

            return View(model);
        }

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
                    name = product?.Name ?? this.L("(товар видалено)");
                }
                else
                {
                    var business = await _businessService.GetByIdAsync(entityId);
                    name = business?.StoreName ?? this.L("(бізнес видалено)");
                }

                scheduledJobs.Add(new ScheduledModerationJobViewModel
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    ProductName = name,
                    NextRunAtUtc = nextFireTime
                });
            }

            var recurringJobs = new List<RecurringJobViewModel>();
            var cleanupJobKey = new JobKey("pending-order-expiry-job", "cleanup");
            if (await scheduler.CheckExists(cleanupJobKey))
            {
                var cleanupTriggers = await scheduler.GetTriggersOfJob(cleanupJobKey);
                var nextRun = cleanupTriggers
                    .Select(t => t.GetNextFireTimeUtc())
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value.UtcDateTime)
                    .OrderBy(t => t)
                    .Cast<DateTime?>()
                    .FirstOrDefault();
                var previousRun = cleanupTriggers
                    .Select(t => t.GetPreviousFireTimeUtc())
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value.UtcDateTime)
                    .OrderByDescending(t => t)
                    .Cast<DateTime?>()
                    .FirstOrDefault();

                recurringJobs.Add(new RecurringJobViewModel
                {
                    GroupName = cleanupJobKey.Group,
                    JobName = cleanupJobKey.Name,
                    DisplayName = this.L("Автоскасування прострочених замовлень"),
                    Description = this.L("Щогодини скасовує замовлення зі статусом «Очікує підтвердження», якщо оплата не надійшла протягом налаштованого часу (за замовчуванням 48 год.)."),
                    NextRunAtUtc = nextRun,
                    PreviousRunAtUtc = previousRun
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

            var openReturnRequests = await _postSaleService.GetAllOpenRequestsAsync();
            foreach (var request in openReturnRequests)
            {
                request.UnreadFromBuyer = await _refundChatService.GetUnreadCountAsync(request.Id, isSellerChannel: false, byAdmin: true);
                request.UnreadFromSeller = await _refundChatService.GetUnreadCountAsync(request.Id, isSellerChannel: true, byAdmin: true);
            }

            var openTickets = await _supportTicketService.GetOpenTicketsForAdminAsync();

            var adultRequests = (await _adminService.GetPendingAdultRequestsAsync())
                .Select(b => new AdultRequestViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Owner = b.Owner,
                    LogoUrl = b.LogoUrl
                })
                .ToList();

            return View(new JobsPageViewModel
            {
                IsSchedulerRunning = scheduler.IsStarted && !scheduler.InStandbyMode && !scheduler.IsShutdown,
                ScheduledJobs = scheduledJobs.OrderBy(j => j.NextRunAtUtc).ToList(),
                RecurringJobs = recurringJobs,
                RecentDecisions = recentDecisions,
                HasCustomApiKey = !string.IsNullOrEmpty(await _settingsService.GetAsync("GoogleAI", "ApiKey")),
                ModerationModel = await _settingsService.GetAsync("GoogleAI", "Model"),
                OpenReturnRequests = openReturnRequests,
                OpenTickets = openTickets,
                AdultRequests = adultRequests
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateModerationApiKey(string? apiKey, string? model)
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                await _settingsService.SetAsync("GoogleAI", "ApiKey", apiKey, isSecret: true, actorUserId: CurrentAdminId);
            }
            if (!string.IsNullOrWhiteSpace(model))
            {
                await _settingsService.SetAsync("GoogleAI", "Model", model, isSecret: false, actorUserId: CurrentAdminId);
            }

            TempData["Success"] = this.L("Налаштування Gemini API оновлено.");
            return RedirectToAction("Jobs");
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
                TempData["Error"] = this.L("Перевірте введені значення - деякі з них поза допустимими межами.");
                return View(model);
            }

            await _settingsService.SetAsync("BusinessRules", "MaxProductsPerBusiness", model.MaxProductsPerBusiness.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "MaxDiscountPercent", model.MaxDiscountPercent.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "MaxPromoCodePercent", model.MaxPromoCodePercent.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "MaxActivePromoCodesPerBusiness", model.MaxActivePromoCodesPerBusiness.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "PendingOrderExpiryHours", model.PendingOrderExpiryHours.ToString(), false, CurrentAdminId);
            await _settingsService.SetAsync("BusinessRules", "LowStockThreshold", model.LowStockThreshold.ToString(), false, CurrentAdminId);

            TempData["Success"] = this.L("Ліміти бізнесу збережено.");
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
            var overview = await _adminService.GetStatisticsAsync(filter.StartDate, filter.EndDate, filter.BusinessId);
            var businesses = await _adminService.GetBusinessesAsync();

            return View(new AdminStatisticsPageViewModel
            {
                Filter = filter,
                Overview = overview,
                Businesses = businesses.OrderBy(b => b.Name).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeBusinessCommission(Guid businessId, decimal commissionPercent, string? returnUrl = null)
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
                TempData["Error"] = this.L(result.Message);
            }
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
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
        public async Task<IActionResult> CreateCategory(string name, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = this.L("Назва категорії не може бути порожньою.");
                return RedirectToAction("Categories");
            }

            var result = await _categoryService.CreateCategory(name.Trim(), imageUrl);
            if (result.Succeeded)
            {
                TempData["Success"] = this.L("Категорію «{0}» створено.", name);
            }
            else
            {
                TempData["Error"] = this.L(result.Message);
            }

            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory(Guid categoryId, string name, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = this.L("Назва категорії не може бути порожньою.");
                return RedirectToAction("Categories");
            }

            var result = await _categoryService.UpdateCategory(name.Trim(), categoryId, imageUrl);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? this.L("Категорію оновлено.") : this.L(result.Message);

            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> SetCategoryAdultStatus(Guid categoryId, bool isAdultOnly)
        {
            var result = await _categoryService.SetCategoryAdultStatus(categoryId, isAdultOnly);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? (isAdultOnly ? this.L("Категорію позначено як 18+.") : this.L("Позначку 18+ знято з категорії."))
                : result.Message;

            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(Guid categoryId)
        {
            var result = await _categoryService.DeleteCategory(categoryId);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? this.L("Категорію видалено.") : this.L(result.Message);

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
                TempData["Error"] = this.L("Планувальник вже призупинено.");
                return RedirectToAction("Jobs");
            }

            await scheduler.Standby();

            await _activityLog.LogUserActionAsync(CurrentAdminId!, "SchedulerPaused",
                "Планувальник задач модерації призупинено адміністратором.",
                actorUserId: CurrentAdminId);

            TempData["Success"] = this.L("Планувальник призупинено. Нові задачі не запускатимуться до відновлення.");
            return RedirectToAction("Jobs");
        }

        [HttpPost]
        public async Task<IActionResult> ResumeScheduler()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            if (!scheduler.InStandbyMode)
            {
                TempData["Error"] = this.L("Планувальник і так активний.");
                return RedirectToAction("Jobs");
            }

            await scheduler.Start();

            await _activityLog.LogUserActionAsync(CurrentAdminId!, "SchedulerResumed",
                "Планувальник задач модерації відновлено адміністратором.",
                actorUserId: CurrentAdminId);

            TempData["Success"] = this.L("Планувальник відновлено.");
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
                TempData["Error"] = this.L("Задачу не знайдено в черзі — можливо, вона вже виконана.");
                return RedirectToAction("Jobs");
            }

            await scheduler.TriggerJob(jobKey);

            await _activityLog.LogUserActionAsync(CurrentAdminId!, "ModerationJobTriggeredManually",
                $"Модерацію ({entityType}) {entityId} запущено вручну адміністратором.",
                actorUserId: CurrentAdminId);

            TempData["Success"] = this.L("Задачу запущено негайно.");
            return RedirectToAction("Jobs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReturnRequest(Guid requestId)
        {
            var result = await _postSaleService.AdminApproveAsync(requestId);
            if (result.Succeeded)
            {
                await _activityLog.LogUserActionAsync(CurrentAdminId!, "ReturnApprovedByAdmin",
                    "Адміністратор підтвердив заявку на повернення, очікуємо продавця.",
                    actorUserId: CurrentAdminId);
            }

            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);
            return RedirectToAction("Jobs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReturnRequest(Guid requestId, string reason)
        {
            var result = await _postSaleService.AdminRejectAsync(requestId, reason);
            if (result.Succeeded)
            {
                await _activityLog.LogUserActionAsync(CurrentAdminId!, "ReturnRejectedByAdmin",
                    $"Адміністратор відхилив заявку на повернення: {reason}",
                    actorUserId: CurrentAdminId);
            }

            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);
            return RedirectToAction("Jobs");
        }

        [HttpGet]
        public async Task<IActionResult> JobsCounts()
        {
            var returnCount = (await _postSaleService.GetAllOpenRequestsAsync()).Count;
            var ticketCount = (await _supportTicketService.GetOpenTicketsForAdminAsync()).Count;
            var adultCount = (await _adminService.GetPendingAdultRequestsAsync()).Count;

            return Json(new { returnCount, ticketCount, adultCount });
        }

        [HttpPost]
        public async Task<IActionResult> RunRecurringJobNow(string groupName, string jobName)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName, groupName);

            if (!await scheduler.CheckExists(jobKey))
            {
                TempData["Error"] = this.L("Задачу не знайдено.");
                return RedirectToAction("Jobs");
            }

            await scheduler.TriggerJob(jobKey);

            await _activityLog.LogUserActionAsync(CurrentAdminId!, "RecurringJobTriggeredManually",
                $"Задачу «{jobName}» запущено вручну адміністратором (позачергово, регулярний розклад не порушено).",
                actorUserId: CurrentAdminId);

            TempData["Success"] = this.L("Задачу запущено негайно.");
            return RedirectToAction("Jobs");
        }
        [HttpGet]
        public async Task<IActionResult> Reviews([FromQuery] AdminReviewFilterViewModel filter)
        {
            var tab = filter.Tab?.ToLowerInvariant() switch
            {
                "published" => "published",
                "rejected" => "rejected",
                "appeals" => "appeals",
                _ => "all"
            };
            filter.Tab = tab;

            ReviewStatus? status = tab switch
            {
                "published" => ReviewStatus.Approved,
                "rejected" => ReviewStatus.Rejected,
                _ => null
            };

            var result = await _adminService.GetFilteredReviewsAsync(new ReviewAdminQueryDto
            {
                Search = filter.Search,
                Status = status,
                Pagination = new PaginationParams { Page = filter.Page, PageSize = filter.PageSize }
            });

            var totalCount = (await _adminService.GetFilteredReviewsAsync(new ReviewAdminQueryDto
            {
                Pagination = new PaginationParams { Page = 1, PageSize = 1 }
            })).TotalItems;
            var rejectedCount = (await _adminService.GetFilteredReviewsAsync(new ReviewAdminQueryDto
            {
                Status = ReviewStatus.Rejected,
                Pagination = new PaginationParams { Page = 1, PageSize = 1 }
            })).TotalItems;

            return View(new AdminReviewsPageViewModel
            {
                Filter = filter,
                Result = result,
                Appeals = await _adminService.GetPendingAppealsAsync(),
                TotalCount = totalCount,
                RejectedCount = rejectedCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingAppeals()
        {
            var appeals = await _adminService.GetPendingAppealsAsync();
            var appealViewModels = appeals.Select(a => new ReviewAppealAdminDto
            {
                Id = a.Id,
                ReviewId = a.ReviewId,
                ProductId = a.ProductId,
                ProductName = a.ProductName,
                UserId = a.UserId,
                UserName = a.UserName,
                OldBody = a.OldBody,
                OldRating = a.OldRating,
                RejectionReason = a.RejectionReason,
                NewBody = a.NewBody,
                NewRating = a.NewRating,
                CreatedAt = a.CreatedAt
            }).ToList();
            return Ok(appealViewModels);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessAppeal(Guid appealId, bool approve, string? reason)
        {
            var result = await _adminService.ProcessAppeal(appealId, approve, CurrentAdminId ?? string.Empty, reason);
            return ModerationResult(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(Guid reviewId)
        {
            var result = await _adminService.DeleteReview(reviewId);
            return ModerationResult(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReview(Guid reviewId, string reason)
        {
            var result = await _adminService.RejectReview(reviewId, CurrentAdminId ?? string.Empty, reason);
            return ModerationResult(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(Guid reviewId, string body)
        {
            var result = await _adminService.EditReview(reviewId, CurrentAdminId ?? string.Empty, body);
            return ModerationResult(result);
        }

        private IActionResult ModerationResult(ServiceResult result)
        {
            var response = new { message = this.L(result.Message) };
            if (result.Succeeded)
            {
                return Ok(response);
            }

            if (result.Message.Contains("не знайдено", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(response);
            }

            if (result.Message.Contains("вже було оброблено", StringComparison.OrdinalIgnoreCase)
                || result.Message.Contains("очікує розгляду", StringComparison.OrdinalIgnoreCase)
                || result.Message.Contains("лише для відхиленого", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(response);
            }

            return BadRequest(response);
        }

    }
}
