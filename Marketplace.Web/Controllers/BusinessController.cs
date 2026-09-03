using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Web.Models;
using Marketplace.Web.Models.Businesses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class BusinessController : Controller
    {
        private readonly IBusinessService _businessService;
        private readonly IBusinessAccessService _businessAccessService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IReviewService _reviewService;
        private readonly IPostSaleService _postSaleService;
        private readonly IPromoCodeService _promoCodeService;
        private readonly IDiscountService _discountService;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly IProductAnalyticsService _productAnalyticsService;
        private readonly ISystemSettingsService _settingsService;

        public BusinessController(
            IBusinessService businessService,
            IBusinessAccessService businessAccessService,
            IProductService productService,
            IOrderService orderService,
            IReviewService reviewService,
            IPostSaleService postSaleService,
            IPromoCodeService promoCodeService,
            IDiscountService discountService,
            UserManager<User> userManager,
            IEmailSender emailSender,
            AppDbContext context,
            IActivityLogService activityLog,
            IProductAnalyticsService productAnalyticsService,
            ISystemSettingsService settingsService)
        {
            _businessService = businessService;
            _businessAccessService = businessAccessService;
            _productService = productService;
            _orderService = orderService;
            _reviewService = reviewService;
            _postSaleService = postSaleService;
            _promoCodeService = promoCodeService;
            _discountService = discountService;
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
            _activityLog = activityLog;
            _productAnalyticsService = productAnalyticsService;
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var businesses = await _businessAccessService.GetAccessibleBusinessesAsync(userId);
            var businessCards = new List<BusinessDashboardItemViewModel>();

            foreach (var business in businesses)
            {
                var products = await _productService.GetByBusinessAsync(business.Id);
                var orderSummary = await _orderService.GetBusinessOrderSummary(business.Id);
                businessCards.Add(new BusinessDashboardItemViewModel
                {
                    Id = business.Id,
                    StoreName = business.StoreName,
                    Description = business.Description,
                    LogoUrl = business.LogoUrl,
                    BannerUrl = business.BannerUrl,
                    Status = business.Status,
                    IsOwner = business.OwnerId == userId,
                    ProductCount = products.Count,
                    ActiveProductCount = products.Count(product => product.Status == Marketplace.Domain.Enums.ProductStatus.Active),
                    PendingProductCount = products.Count(product => product.Status == Marketplace.Domain.Enums.ProductStatus.Pending),
                    OutOfStockProductCount = products.Count(product => product.Quantity <= 0),
                    OrderCount = orderSummary.TotalOrders,
                    AwaitingOrderCount = orderSummary.AwaitingProcessingOrders,
                    SoldItemCount = orderSummary.SoldItems,
                    Revenue = orderSummary.Revenue
                });
            }

            return View(new BusinessDashboardViewModel
            {
                Businesses = businessCards,
                CanCreateBusiness = businesses.All(business => business.OwnerId != userId)
            });
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(Guid businessId, DateTime? startDate, DateTime? endDate)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var end = (endDate ?? DateTime.UtcNow).Date;
            var start = (startDate ?? end.AddDays(-29)).Date;
            var periodLength = (end - start).Days + 1;
            var previousEnd = start.AddDays(-1);
            var previousStart = previousEnd.AddDays(-(periodLength - 1));

            var statsResult = await _businessService.GetBusinessStatsByPeriodAsync(businessId, start, end);
            var previousStatsResult = await _businessService.GetBusinessStatsByPeriodAsync(businessId, previousStart, previousEnd);
            var productsStatsResult = await _businessService.GetProductsStatsByPeriodAsnyc(businessId, start, end);
            var customersStatsResult = await _businessService.GetCustomersStatsByPeriodAsync(businessId, start, end);
            var balance = await _businessService.GetBusinessBalance(businessId);
            var allTimeSummary = await _orderService.GetBusinessOrderSummary(businessId);
            var products = await _productService.GetByBusinessAsync(businessId);
            var lowStockThreshold = await _settingsService.GetIntAsync("BusinessRules", "LowStockThreshold", 5);
            var traffic = await _productAnalyticsService.GetBusinessTrafficStatsAsync(businessId, start, end);
            var recentActivity = await _activityLog.GetRecentBusinessActionsAsync(businessId, take: 15);

            var periodRevenue = statsResult.Succeeded ? statsResult.Data.TotalRevenue : 0;
            var periodOrders = statsResult.Succeeded ? statsResult.Data.Stats.Sum(s => s.OrderCount) : 0;
            var periodAov = statsResult.Succeeded ? (double)statsResult.Data.AOV : 0;

            var previousRevenue = previousStatsResult.Succeeded ? previousStatsResult.Data.TotalRevenue : 0;
            var previousOrders = previousStatsResult.Succeeded ? previousStatsResult.Data.Stats.Sum(s => s.OrderCount) : 0;
            var previousAov = previousStatsResult.Succeeded ? (double)previousStatsResult.Data.AOV : 0;

            static double? PercentDelta(double current, double previous)
            {
                if (previous == 0)
                {
                    return current == 0 ? 0 : null;
                }

                return Math.Round((current - previous) * 100d / previous, 1);
            }

            var viewsByProduct = traffic.ByProduct.ToDictionary(p => p.ProductId, p => p.Views);

            var model = new BusinessDashboardPageViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                Filter = new BusinessDashboardFilterViewModel { BusinessId = businessId, StartDate = start, EndDate = end },

                Chart = statsResult.Succeeded
                    ? statsResult.Data.Stats
                        .OrderBy(s => s.Date)
                        .Select(s => new BusinessDashboardChartPointViewModel { Date = s.Date, Revenue = s.Revenue, OrderCount = s.OrderCount })
                        .ToList()
                    : new List<BusinessDashboardChartPointViewModel>(),
                PeriodRevenue = periodRevenue,
                PeriodOrders = periodOrders,
                PeriodAverageOrderValue = periodAov,

                RevenueDeltaPercent = PercentDelta((double)periodRevenue, (double)previousRevenue),
                OrdersDeltaPercent = PercentDelta(periodOrders, previousOrders),
                AverageOrderValueDeltaPercent = PercentDelta(periodAov, previousAov),

                Balance = balance,
                AllTimeRevenue = allTimeSummary.Revenue,
                AllTimeOrders = allTimeSummary.TotalOrders,
                AllTimeSoldItems = allTimeSummary.SoldItems,
                AwaitingOrders = allTimeSummary.AwaitingProcessingOrders,

                TotalProductCount = products.Count,
                ActiveProductCount = products.Count(p => p.Status == Marketplace.Domain.Enums.ProductStatus.Active),
                PendingProductCount = products.Count(p => p.Status == Marketplace.Domain.Enums.ProductStatus.Pending),
                OutOfStockProductCount = products.Count(p => p.Quantity <= 0),
                LowStockProductCount = products.Count(p => p.Quantity > 0 && p.Quantity <= lowStockThreshold),

                TotalViews = traffic.TotalViews,
                TotalCartAdds = traffic.TotalCartAdds,
                TotalWishlistAdds = traffic.TotalWishlistAdds,

                TopProducts = productsStatsResult.Succeeded
                    ? productsStatsResult.Data.Stats
                        .OrderByDescending(s => s.Revenue)
                        .Take(5)
                        .Select(s => new BusinessDashboardTopProductViewModel
                        {
                            ProductId = s.ProductId,
                            ProductName = s.ProductName,
                            Revenue = s.Revenue,
                            UnitsSold = s.TotalSales,
                            Views = viewsByProduct.TryGetValue(s.ProductId, out var views) ? views : 0
                        })
                        .ToList()
                    : new List<BusinessDashboardTopProductViewModel>(),

                TopCustomers = customersStatsResult.Succeeded
                    ? customersStatsResult.Data.Stats
                        .OrderByDescending(s => s.Revenue)
                        .Take(5)
                        .Select(s => new BusinessDashboardTopCustomerViewModel
                        {
                            UserName = s.UserName,
                            Revenue = s.Revenue,
                            PurchaseCount = s.PurchaseCount
                        })
                        .ToList()
                    : new List<BusinessDashboardTopCustomerViewModel>(),

                RecentActivity = recentActivity
                    .Select(a => new BusinessDashboardActivityItemViewModel { Description = a.Description, CreatedAt = a.CreatedAt })
                    .ToList()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BusinessCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var result = await _businessService.CreateBusiness(new BusinessCreateDto
            {
                StoreName = model.StoreName,
                Description = model.Description,
                UserId = userId!,
                Logo = model.Logo,
                Banner = model.Banner
            });

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            TempData["Success"] = "Бізнес створено та відправлено на перевірку.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Orders(Guid businessId, OrderStatus? status)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var orders = await _orderService.GetBusinessOrders(businessId, status);

            return View(new BusinessOrdersPageViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                StatusFilter = status,
                Orders = orders.Select(order => new BusinessOrderListItemViewModel
                {
                    OrderId = order.OrderId,
                    CreatedAt = order.CreatedAt,
                    Status = order.Status,
                    BuyerName = order.BuyerName,
                    BuyerEmail = order.BuyerEmail,
                    ShippingAddress = $"{order.ShippingCity}, {order.ShippingStreet}",
                    ItemsCount = order.ItemsCount,
                    LineTotal = order.LineTotal
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(Guid businessId, Guid orderId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var order = await _orderService.GetBusinessOrderDetails(businessId, orderId);
            if (order is null)
            {
                return NotFound();
            }

            var postSaleRequests = await _postSaleService.GetRequestsForOrderAsync(orderId);

            return View(new BusinessOrderDetailsViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                OrderId = order.OrderId,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                OrderTotalAmount = order.OrderTotalAmount,
                BuyerName = order.BuyerName,
                BuyerEmail = order.BuyerEmail,
                BuyerPhone = order.BuyerPhone,
                ShippingCountry = order.ShippingCountry,
                ShippingCity = order.ShippingCity,
                ShippingStreet = order.ShippingStreet,
                Items = order.Items.Select(item => new BusinessOrderLineViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ImageUrl = item.ImageUrl,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    BelongsToBusiness = item.BelongsToBusiness
                }).ToList(),
                PostSaleRequests = postSaleRequests.Select(r => new BusinessPostSaleRequestViewModel
                {
                    Id = r.Id,
                    ProductName = r.ProductName,
                    ImageUrl = r.ImageUrl,
                    Reason = r.Reason,
                    Comment = r.Comment,
                    PhotoUrls = r.PhotoUrls,
                    Status = r.Status,
                    SellerResponse = r.SellerResponse,
                    CreatedAt = r.CreatedAt
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolvePostSaleRequest(Guid businessId, Guid orderId, Guid requestId, PostSaleRequestStatus decision, string? sellerResponse)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var result = await _postSaleService.ResolveRequestAsync(requestId, businessId, decision, sellerResponse);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "PostSaleRequestResolved",
                    $"Заявку на повернення вирішено: {decision}.",
                    actorUserId: _userManager.GetUserId(User), relatedEntityId: requestId);

                if (decision == PostSaleRequestStatus.Approved && result.EntityId.HasValue)
                {
                    // Approving a return refunds just that one line item — restores its stock,
                    // gives back the seller's net share, and reverses the marketplace's
                    // commission at the rate that applied when the order was confirmed. Other
                    // items in the same (possibly multi-seller) order are untouched.
                    var refundResult = await _orderService.RefundOrderItemAsync(result.EntityId.Value);
                    if (!refundResult.Succeeded)
                    {
                        TempData["Error"] = $"Заявку підтверджено, але не вдалось повернути кошти: {refundResult.Message}";
                        return RedirectToAction(nameof(OrderDetails), new { businessId, orderId });
                    }
                }
            }
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Рішення щодо заявки збережено."
                : result.Message;

            return RedirectToAction(nameof(OrderDetails), new { businessId, orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderStatus(Guid businessId, Guid orderId, OrderStatus status)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            // GetBusinessOrderDetails also acts as the ownership check: it returns null unless
            // this business actually has a line item in the order, so a seller can't change the
            // status of an order that has nothing to do with their store.
            var order = await _orderService.GetBusinessOrderDetails(businessId, orderId);
            if (order is null)
            {
                return NotFound();
            }

            var result = await _orderService.ChangeOrderStatus(orderId, status);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                await _activityLog.LogBusinessActionAsync(businessId, "OrderStatusChanged",
                    $"Статус замовлення змінено на «{status}».",
                    actorUserId: _userManager.GetUserId(User), relatedEntityId: orderId);
                TempData["Success"] = "Статус замовлення оновлено.";
            }

            return RedirectToAction(nameof(OrderDetails), new { businessId, orderId });
        }

        [HttpGet]
        public async Task<IActionResult> Team(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var userId = _userManager.GetUserId(User)!;
            var canManage = await _businessAccessService.HasAccessAsync(businessId, userId, BusinessMemberRole.Owner);
            var owner = await _userManager.FindByIdAsync(business.OwnerId);

            var members = await _context.BusinessMembers
                .Include(m => m.User)
                .Where(m => m.BusinessId == businessId)
                .OrderByDescending(m => m.IsAccepted)
                .ThenBy(m => m.Role)
                .ToListAsync();

            return View(new BusinessTeamViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                OwnerEmail = owner?.Email ?? string.Empty,
                CanManage = canManage,
                Members = members.Select(m => new BusinessTeamMemberViewModel
                {
                    Id = m.Id,
                    Email = m.User?.Email ?? "невідомо",
                    Role = m.Role,
                    IsAccepted = m.IsAccepted
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid businessId, Guid memberId)
        {
            var userId = _userManager.GetUserId(User)!;
            var canManage = await _businessAccessService.HasAccessAsync(businessId, userId, BusinessMemberRole.Owner);
            if (!canManage)
            {
                return Forbid();
            }

            var member = await _context.BusinessMembers
                .FirstOrDefaultAsync(m => m.Id == memberId && m.BusinessId == businessId);

            if (member is null)
            {
                return NotFound();
            }

            member.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Учасника видалено з команди.";
            return RedirectToAction(nameof(Team), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> Finance(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var summary = await _orderService.GetBusinessOrderSummary(businessId);
            var orders = await _orderService.GetBusinessOrders(businessId);

            return View(new BusinessFinanceViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                Revenue = summary.Revenue,
                SoldItems = summary.SoldItems,
                TotalOrders = summary.TotalOrders,
                RecentSales = orders
                    .OrderByDescending(order => order.CreatedAt)
                    .Take(10)
                    .Select(order => new BusinessOrderListItemViewModel
                    {
                        OrderId = order.OrderId,
                        CreatedAt = order.CreatedAt,
                        Status = order.Status,
                        BuyerName = order.BuyerName,
                        BuyerEmail = order.BuyerEmail,
                        ShippingAddress = $"{order.ShippingCity}, {order.ShippingStreet}",
                        ItemsCount = order.ItemsCount,
                        LineTotal = order.LineTotal
                    }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Performance(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var orders = await _orderService.GetBusinessOrders(businessId);
            var distinctOrders = orders.GroupBy(o => o.OrderId).Select(g => g.First()).ToList();

            var products = await _productService.GetByBusinessAsync(businessId);
            var allRatings = new List<double>();
            foreach (var product in products)
            {
                var reviews = await _reviewService.GetReviewsByProductId(product.Id);
                allRatings.AddRange(reviews.Select(r => (double)r.Rating));
            }

            return View(new BusinessPerformanceViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                Status = business.Status,
                TotalOrders = distinctOrders.Count,
                CanceledOrders = distinctOrders.Count(o => o.Status == OrderStatus.Canceled),
                AverageRating = allRatings.Count == 0 ? 0 : Math.Round(allRatings.Average(), 1),
                ReviewCount = allRatings.Count,
                ProductCount = products.Count
            });
        }

        [HttpGet]
        public async Task<IActionResult> PromoCodes(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var promoCodes = await _promoCodeService.GetForBusinessAsync(businessId);
            var businessDiscount = await _discountService.GetBusinessDiscount(businessId);

            return View(new BusinessPromoCodesPageViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                PromoCodes = promoCodes,
                BusinessDiscount = businessDiscount
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetBusinessDiscount(Guid businessId, decimal discountPercent, DateTime discountStartDate, DateTime discountEndDate)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var result = await _discountService.SetBusinessDiscount(businessId, discountPercent, discountStartDate, discountEndDate);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "BusinessDiscountSet",
                    $"Встановлено знижку {discountPercent}% на весь магазин «{business.StoreName}».",
                    actorUserId: _userManager.GetUserId(User));
            }
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(PromoCodes), new { businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBusinessDiscount(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var result = await _discountService.RemoveBusinessDiscount(businessId);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "BusinessDiscountRemoved",
                    $"Скасовано знижку на весь магазин «{business.StoreName}».",
                    actorUserId: _userManager.GetUserId(User));
            }
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(PromoCodes), new { businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromoCode(Guid businessId, string code, int percent, DateTime startDate, DateTime endDate)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var result = await _promoCodeService.CreateAsync(businessId, code, percent, startDate, endDate);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "PromoCodeCreated",
                    $"Створено промокод «{code}» (знижка {percent}%).",
                    actorUserId: _userManager.GetUserId(User), relatedEntityId: result.EntityId);
            }
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(PromoCodes), new { businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivatePromoCode(Guid businessId, Guid promoCodeId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var result = await _promoCodeService.DeactivateAsync(businessId, promoCodeId);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "PromoCodeDeactivated",
                    "Промокод деактивовано.",
                    actorUserId: _userManager.GetUserId(User), relatedEntityId: promoCodeId);
            }
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(PromoCodes), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> Growth(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            return View(new BusinessGrowthViewModel { BusinessId = businessId, StoreName = business.StoreName });
        }

        [HttpGet]
        public async Task<IActionResult> Integrations(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            return View(new BusinessIntegrationsViewModel { BusinessId = businessId, StoreName = business.StoreName });
        }

        [HttpGet]
        public async Task<IActionResult> Settings(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            return View(new BusinessSettingsViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                Description = business.Description
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(BusinessSettingsViewModel model)
        {
            var business = await GetAccessibleBusinessOrNullAsync(model.BusinessId);
            if (business is null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            business.StoreName = model.StoreName.Trim();
            business.Description = model.Description.Trim();
            _context.Businesses.Update(business);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Дані магазину оновлено.";
            return RedirectToAction(nameof(Settings), new { businessId = model.BusinessId });
        }

        private async Task<Business?> GetAccessibleBusinessOrNullAsync(Guid businessId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var businesses = await _businessAccessService.GetAccessibleBusinessesAsync(userId);
            return businesses.FirstOrDefault(b => b.Id == businessId);
        }

        [HttpGet("business/{businessId}/add-member")]
        public IActionResult AddMember(Guid businessId)
        {
            var model = new BusinessMemberCreateViewModel { BusinessId = businessId };

            return View(model);
        }

        [HttpPost("business/{businessId}/add-member")]
        public async Task<IActionResult> AddMember(BusinessMemberCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _businessService.AddBusinessMember(new BusinessMemberCreateDto
            {
                BusinessId = model.BusinessId,
                UserId = model.EmployeeId,
                Role = model.Role
            });

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            var approveLink = Url.Action("ApproveMembership", "Business", new { businessId = model.BusinessId, userId = model.EmployeeId, token = result.Token, role = model.Role }, Request.Scheme);
            var denyLink = Url.Action("DenyMembership", "Business", new { businessId = model.BusinessId, userId = model.EmployeeId, token = result.Token, role = model.Role }, Request.Scheme);

            await _emailSender.SendEmailAsync(result.Email!, "Запит на членство",
                $"Вам було надіслано запит на членство: " +
                $"<a href='{approveLink}'>Прийняти</a>" +
                $"<a href='{denyLink}'>Відхилити</a>");

            TempData["Success"] = "Запрошення надіслано.";
            return RedirectToAction(nameof(Team), new { businessId = model.BusinessId });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ApproveMembership(Guid businessId, string userId, string token, BusinessMemberRole role)
        {
            var result = await _businessService.ApproveMembership(userId, businessId, token, role);
            return RedirectToAction("MembershipResult", new { message = result.Message });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> DenyMembership(Guid businessId, string userId, string token, BusinessMemberRole role)
        {
            var result = await _businessService.DenyMembership(userId, businessId, token, role);
            return RedirectToAction("MembershipResult", new { message = result.Message });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult MembershipResult(string message)
        {
            ViewBag.Message = message;

            return View();
        }

        [HttpPost("business/{businessId}/business-stats")]
        public async Task<IActionResult> GetBusinessStatsByPeriod(BusinessStatsQueryViewModel query)
        {
            var result = await _businessService.GetBusinessStatsByPeriodAsync(
                query.BusinessId, query.StartDate, query.EndDate);

            var pageModel = new BusinessStatsPageViewModel { Query = query };

            if (!result.Succeeded)
            {
                pageModel.ErrorMessage = result.Message;
                return View(pageModel);
            }

            pageModel.Stats = result.Data;
            return View(pageModel);
        }

        [HttpPost("business/{businessId}/products-stats")]
        public async Task<IActionResult> GetProductsStatsByPeriod(ProductsStatsQueryViewModel query)
        {
            var result = await _businessService.GetProductsStatsByPeriodAsnyc(
                query.BusinessId, query.StartDate, query.EndDate);

            var pageModel = new ProductsStatsPageViewModel { Query = query };

            if (!result.Succeeded)
            {
                pageModel.ErrorMessage = result.Message;
                return View(pageModel);
            }

            pageModel.Stats = result.Data;
            return View(pageModel);
        }

        [HttpPost("business/{businessId}/customers-stats")]
        public async Task<IActionResult> GetCustomersStatsByPeriod(CustomersStatsQueryViewModel query)
        {
            var result = await _businessService.GetCustomersStatsByPeriodAsync(
                query.BusinessId, query.StartDate, query.EndDate);

            var pageModel = new CustomersStatsPageViewModel { Query = query };

            if (!result.Succeeded)
            {
                pageModel.ErrorMessage = result.Message;
                return View(pageModel);
            }

            pageModel.Stats = result.Data;
            return View(pageModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetBusinessBalance(Guid businessId)
        {
            var hasAcess = await _businessAccessService.HasAccessAsync(businessId, _userManager.GetUserId(User)!, BusinessMemberRole.Owner);
            if (!hasAcess)
            {
                return Forbid();
            }

            var balance = await _businessService.GetBusinessBalance(businessId);
            return Json(new { businessId, balance });
        }
    }
}