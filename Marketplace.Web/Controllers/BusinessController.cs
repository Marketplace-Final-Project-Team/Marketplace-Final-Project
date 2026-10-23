using Marketplace.Web.Infrastructure;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Web.Models;
using Marketplace.Web.Models.Businesses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using BusinessPermissions = Marketplace.BLL.Common.BusinessPermissions;
using AdultContentStatus = Marketplace.Domain.Enums.AdultContentStatus;

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
        private readonly IActivityLogService _activityLog;
        private readonly IProductAnalyticsService _productAnalyticsService;
        private readonly ISystemSettingsService _settingsService;
        private readonly ICategoryService _categoryService;
        private readonly IRefundChatService _refundChatService;
        private readonly IUserDiscountService _userDiscountService;
        private readonly IWithdrawalService _withdrawalService;

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
            IActivityLogService activityLog,
            IProductAnalyticsService productAnalyticsService,
            ISystemSettingsService settingsService,
            ICategoryService categoryService,
            IRefundChatService refundChatService,
            IUserDiscountService userDiscountService,
            IWithdrawalService withdrawalService)
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
            _activityLog = activityLog;
            _productAnalyticsService = productAnalyticsService;
            _settingsService = settingsService;
            _categoryService = categoryService;
            _refundChatService = refundChatService;
            _userDiscountService = userDiscountService;
            _withdrawalService = withdrawalService;
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
                var orderSummary = await _orderService.GetBusinessOrderSummaryAsync(business.Id);

                var currentRole = await _businessAccessService.GetUserRoleAsync(business.Id, userId)
                    ?? BusinessMemberRole.Owner;

                businessCards.Add(new BusinessDashboardItemViewModel
                {
                    Id = business.Id,
                    StoreName = business.StoreName,
                    Description = business.Description,
                    LogoUrl = business.LogoUrl,
                    BannerUrl = business.BannerUrl,
                    Status = business.Status,
                    IsOwner = business.OwnerId == userId,
                    CurrentUserRole = currentRole,
                    ProductCount = products.Count,
                    ActiveProductCount = products.Count(product => product.Status == ProductStatus.Active),
                    PendingProductCount = products.Count(product => product.Status == ProductStatus.Pending),
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
                CanCreateBusiness = true
            });
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(Guid businessId, DateTime? startDate, DateTime? endDate)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.Statistics);
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
            var allTimeSummary = await _orderService.GetBusinessOrderSummaryAsync(businessId);
            var products = await _productService.GetByBusinessAsync(businessId);
            var lowStockThreshold = await _settingsService.GetIntAsync("BusinessRules", "LowStockThreshold", 5);
            var traffic = await _productAnalyticsService.GetBusinessTrafficStatsAsync(businessId, start, end);
            var recentActivity = await _activityLog.GetRecentBusinessActionsAsync(businessId, take: 15);

            var periodRevenue = statsResult.Succeeded ? statsResult.Data!.TotalRevenue : 0;
            var periodOrders = statsResult.Succeeded ? statsResult.Data!.Stats.Sum(s => s.OrderCount) : 0;
            var periodAov = statsResult.Succeeded ? (double)statsResult.Data!.AOV : 0;

            var previousRevenue = previousStatsResult.Succeeded ? previousStatsResult.Data!.TotalRevenue : 0;
            var previousOrders = previousStatsResult.Succeeded ? previousStatsResult.Data!.Stats.Sum(s => s.OrderCount) : 0;
            var previousAov = previousStatsResult.Succeeded ? (double)previousStatsResult.Data!.AOV : 0;

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
                    ? statsResult.Data!.Stats
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
                ActiveProductCount = products.Count(p => p.Status == ProductStatus.Active),
                PendingProductCount = products.Count(p => p.Status == ProductStatus.Pending),
                OutOfStockProductCount = products.Count(p => p.Quantity <= 0),
                LowStockProductCount = products.Count(p => p.Quantity > 0 && p.Quantity <= lowStockThreshold),
                BannedProductCount = products.Count(p => p.Status == ProductStatus.Banned),

                AccountStatus = business.Status.ToString(),
                MemberSince = business.CreatedAt,
                AverageRating = await _reviewService.GetAvgRatingByBusinessAsync(businessId),
                ReviewCount = await _reviewService.GetReviewCountByBusinessAsync(businessId),

                TotalViews = traffic.TotalViews,
                TotalCartAdds = traffic.TotalCartAdds,
                TotalWishlistAdds = traffic.TotalWishlistAdds,

                TopProducts = productsStatsResult.Succeeded
                    ? productsStatsResult.Data!.Stats
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
                    ? customersStatsResult.Data!.Stats
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

                CategoryStats = productsStatsResult.Succeeded
                    ? productsStatsResult.Data!.Stats
                        .GroupBy(s => products.FirstOrDefault(p => p.Id == s.ProductId)?.Category?.Name ?? "Без категорії")
                        .Select(g => new BusinessDashboardCategoryStatViewModel
                        {
                            CategoryName = g.Key,
                            Revenue = g.Sum(s => s.Revenue),
                            UnitsSold = g.Sum(s => s.TotalSales)
                        })
                        .OrderByDescending(c => c.Revenue)
                        .ToList()
                    : new List<BusinessDashboardCategoryStatViewModel>(),

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

            var result = await _businessService.CreateBusinessAsync(new BusinessCreateDto
            {
                StoreName = model.StoreName,
                Description = model.Description,
                UserId = userId!,
                Logo = model.Logo,
                Banner = model.Banner
            });

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", this.L(result.Message));
                return View(model);
            }

            TempData["Success"] = this.L("Бізнес створено та відправлено на перевірку.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Orders(Guid businessId, OrderStatus? status, string? search, string? searchEmail, decimal? minAmount, decimal? maxAmount)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var orders = await _orderService.GetBusinessOrders(businessId, status, search, searchEmail, minAmount, maxAmount);

            var pendingReturns = (await _postSaleService.GetRequestsForBusinessAsync(businessId))
                .Where(r => r.Status == PostSaleRequestStatus.AwaitingSellerConfirmation)
                .ToList();

            return View(new BusinessOrdersPageViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                StatusFilter = status,
                Search = search,
                SearchEmail = searchEmail,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                Orders = orders.Select(order =>
                {
                    var urgentReturn = pendingReturns.FirstOrDefault(r => r.OrderId == order.OrderId);

                    return new BusinessOrderListItemViewModel
                    {
                        OrderId = order.OrderId,
                        CreatedAt = order.CreatedAt,
                        Status = order.Status,
                        BuyerName = order.BuyerName,
                        BuyerEmail = order.BuyerEmail,
                        ShippingAddress = $"{order.ShippingCity}, {order.ShippingStreet}",
                        ItemsCount = order.ItemsCount,
                        LineTotal = order.LineTotal,
                        HasUrgentReturn = urgentReturn != null,
                        ReturnDeadline = urgentReturn?.SellerConfirmDeadline
                    };
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

            var postSaleRequestViewModels = new List<BusinessPostSaleRequestViewModel>();
            foreach (var r in postSaleRequests)
            {
                var unreadFromAdmin = r.Status == Marketplace.Domain.Enums.PostSaleRequestStatus.SellerDisputed
                    ? await _refundChatService.GetUnreadCountAsync(r.Id, isSellerChannel: true, byAdmin: false)
                    : 0;

                postSaleRequestViewModels.Add(new BusinessPostSaleRequestViewModel
                {
                    Id = r.Id,
                    ProductName = r.ProductName,
                    ImageUrl = r.ImageUrl,
                    Reason = r.Reason,
                    Comment = r.Comment,
                    PhotoUrls = r.PhotoUrls,
                    Status = r.Status,
                    SellerResponse = r.SellerResponse,
                    CreatedAt = r.CreatedAt,
                    SellerConfirmDeadline = r.SellerConfirmDeadline,
                    UnreadFromAdmin = unreadFromAdmin
                });
            }

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
                    VariantSize = item.VariantSize,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    BelongsToBusiness = item.BelongsToBusiness
                }).ToList(),
                PostSaleRequests = postSaleRequestViewModels
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolvePostSaleRequest(Guid businessId, Guid orderId, Guid requestId, PostSaleRequestStatus decision, string? sellerResponse, string? returnUrl = null)
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
            }

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? this.L("Рішення щодо заявки збережено.")
                : result.Message;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(OrderDetails), new { businessId, orderId });
        }

        [HttpGet]
        public async Task<IActionResult> Returns(Guid businessId, string filter = "all")
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var requests = await _postSaleService.GetRequestsForBusinessAsync(businessId);

            var model = new BusinessReturnsPageViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                Filter = string.IsNullOrWhiteSpace(filter) ? "all" : filter,
                AllCount = requests.Count,
                PendingCount = requests.Count(r => r.Status == PostSaleRequestStatus.Pending),
                RefundedCount = requests.Count(r => r.Status == PostSaleRequestStatus.Refunded),
                RejectedCount = requests.Count(r => r.Status == PostSaleRequestStatus.Rejected)
            };

            IEnumerable<PostSaleRequestDto> filtered = model.Filter switch
            {
                "pending" => requests.Where(r => r.Status == PostSaleRequestStatus.Pending),
                "refunded" => requests.Where(r => r.Status == PostSaleRequestStatus.Refunded),
                "rejected" => requests.Where(r => r.Status == PostSaleRequestStatus.Rejected),
                _ => requests
            };

            model.Items = filtered.Select(r => new BusinessReturnRequestViewModel
            {
                Id = r.Id,
                OrderId = r.OrderId,
                ProductName = r.ProductName,
                ImageUrl = r.ImageUrl,
                BuyerName = r.BuyerName,
                UnitPrice = r.UnitPrice,
                Quantity = r.Quantity,
                Reason = r.Reason,
                Comment = r.Comment,
                PhotoUrls = r.PhotoUrls,
                Status = r.Status,
                SellerResponse = r.SellerResponse,
                CreatedAt = r.CreatedAt
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReturn(Guid businessId, Guid orderId, Guid requestId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Forbid();
            }

            var result = await _postSaleService.SellerConfirmReturnAsync(requestId, businessId);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "ReturnConfirmedBySeller",
                    "Продавець підтвердив повернення, кошти повернуто покупцю.",
                    actorUserId: _userManager.GetUserId(User), relatedEntityId: requestId);
            }

            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);
            return RedirectToAction(nameof(OrderDetails), new { businessId, orderId });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisputeReturn(Guid businessId, Guid orderId, Guid requestId, string reason)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return Json(new { success = false, message = this.L("Немає доступу до цього магазину.") });
            }

            var result = await _postSaleService.SellerDisputeAsync(requestId, businessId, reason);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "ReturnDisputedBySeller",
                    $"Продавець відхилив повернення з обґрунтуванням: {reason}",
                    actorUserId: _userManager.GetUserId(User), relatedEntityId: requestId);
            }

            return Json(new { success = result.Succeeded, message = this.L(result.Message), requestId });
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

            var order = await _orderService.GetBusinessOrderDetails(businessId, orderId);
            if (order is null)
            {
                return NotFound();
            }

            var result = await _orderService.ChangeOrderStatus(orderId, status);
            if (!result.Succeeded)
            {
                TempData["Error"] = this.L(result.Message);
            }
            else
            {
                await _activityLog.LogBusinessActionAsync(businessId, "OrderStatusChanged",
                    $"Статус замовлення змінено на «{status}».",
                    actorUserId: _userManager.GetUserId(User), relatedEntityId: orderId);
                TempData["Success"] = this.L("Статус замовлення оновлено.");
            }

            return RedirectToAction(nameof(OrderDetails), new { businessId, orderId });
        }

        [HttpGet]
        public async Task<IActionResult> Team(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.ManageTeam);
            if (business is null)
            {
                return Forbid();
            }

            var userId = _userManager.GetUserId(User)!;
            var canManage = await _businessAccessService.HasAccessAsync(businessId, userId, BusinessMemberRole.Owner);
            
            var owner = await _userManager.FindByIdAsync(business.OwnerId);
            var members = await _businessService.GetTeamMembersAsync(businessId);

            return View(new BusinessTeamViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                OwnerEmail = owner?.Email ?? string.Empty,
                CanManage = canManage,
                Members = members.Select(m => new BusinessTeamMemberViewModel
                {
                    Id = m.Id,
                    Email = m.Email ?? this.L("невідомо"),
                    Role = m.Role,
                    IsAccepted = m.IsAccepted
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMemberRole(Guid businessId, Guid memberId, BusinessMemberRole newRole)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.ManageTeam);
            if (business is null)
            {
                return Forbid();
            }

            var res = await _businessService.UpdateTeamMemberAsync(memberId, businessId, newRole);

            if (!res.Succeeded) 
            {
                TempData["Error"] = this.L(res.Message);
                return RedirectToAction(nameof(Team), new { businessId });
            }

            await _activityLog.LogBusinessActionAsync(businessId, "MemberRoleUpdated",
                $"Роль учасника змінено на «{newRole}».",
                actorUserId: _userManager.GetUserId(User), relatedEntityId: memberId);

            TempData["Success"] = this.L("Роль учасника успішно оновлено.");
            return RedirectToAction(nameof(Team), new { businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid businessId, Guid memberId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.ManageTeam);
            if (business is null)
            {
                return Forbid();
            }

            var res = await _businessService.RemoveTeamMemberAsync(memberId, businessId);

            if (!res.Succeeded)
            {
                TempData["Error"] = this.L(res.Message);
                return RedirectToAction(nameof(Team), new { businessId });
            }

            TempData["Success"] = this.L("Учасника видалено з команди.");
            return RedirectToAction(nameof(Team), new { businessId });
        }

        [HttpGet("business/{businessId}/add-member")]
        public async Task<IActionResult> AddMember(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.ManageTeam);
            if (business is null)
            {
                return Forbid();
            }

            var model = new BusinessMemberCreateViewModel { BusinessId = businessId };
            return View(model);
        }

        [HttpPost("business/{businessId}/add-member")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(BusinessMemberCreateViewModel model)
        {
            var business = await GetAccessibleBusinessOrNullAsync(model.BusinessId, BusinessPermissions.ManageTeam);
            if (business is null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _businessService.AddBusinessMemberAsync(new BusinessMemberCreateDto
            {
                BusinessId = model.BusinessId,
                UserId = model.EmployeeId,
                Role = model.Role
            });

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", this.L(result.Message));
                return View(model);
            }

            var approveLink = Url.Action("ApproveMembership", "Business", new { businessId = model.BusinessId, userId = model.EmployeeId, token = result.Token, role = model.Role }, Request.Scheme);
            var denyLink = Url.Action("DenyMembership", "Business", new { businessId = model.BusinessId, userId = model.EmployeeId, token = result.Token, role = model.Role }, Request.Scheme);

            await _emailSender.SendEmailAsync(result.Email!, "Запит на членство",
                $"Вам було надіслано запит на членство: " +
                $"<a href='{approveLink}'>Прийняти</a>" +
                $"<a href='{denyLink}'>Відхилити</a>");

            TempData["Success"] = this.L("Запрошення надіслано.");
            return RedirectToAction(nameof(Team), new { businessId = model.BusinessId });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ApproveMembership(Guid businessId, string userId, string token, BusinessMemberRole role)
        {
            var result = await _businessService.ApproveMembershipAsync(userId, businessId, token, role);
            return RedirectToAction("MembershipResult", new { message = this.L(result.Message) });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> DenyMembership(Guid businessId, string userId, string token, BusinessMemberRole role)
        {
            var result = await _businessService.DenyMembershipAsync(userId, businessId, token, role);
            return RedirectToAction("MembershipResult", new { message = this.L(result.Message) });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult MembershipResult(string message)
        {
            ViewBag.Message = message;
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Finance(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.Statistics);
            if (business is null)
            {
                return Forbid();
            }

            var summary = await _orderService.GetBusinessOrderSummaryAsync(businessId);
            var orders = await _orderService.GetBusinessOrders(businessId);

            var trendEnd = DateTime.UtcNow.Date;
            var trendStart = trendEnd.AddDays(-29);
            var trendResult = await _businessService.GetBusinessStatsByPeriodAsync(businessId, trendStart, trendEnd);

            var withdrawals = await _withdrawalService.GetForBusinessAsync(businessId);
            var pendingWithdrawalsTotal = withdrawals
                .Where(w => w.Status == WithdrawalStatus.Pending.ToString())
                .Sum(w => w.Amount);

            var savedCards = await _withdrawalService.GetSavedCardsAsync(businessId);

            var heldBalance = business.HeldBalance;

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
                    }).ToList(),
                RevenueTrend = trendResult.Succeeded
                    ? trendResult.Data!.Stats
                        .OrderBy(s => s.Date)
                        .Select(s => new BusinessDashboardChartPointViewModel { Date = s.Date, Revenue = s.Revenue, OrderCount = s.OrderCount })
                        .ToList()
                    : new List<BusinessDashboardChartPointViewModel>(),
                AvailableBalance = business.Balance - business.HeldBalance,
                PendingWithdrawalsTotal = pendingWithdrawalsTotal,
                Withdrawals = withdrawals.ToList(),
                HeldBalance = heldBalance,
                SavedCards = savedCards,
                ScheduledPayoutEnabled = business.ScheduledPayoutEnabled,
                ScheduledPayoutDayOfMonth = business.ScheduledPayoutDayOfMonth,
                ScheduledPayoutAmount = business.ScheduledPayoutAmount,
                ScheduledPayoutCardId = business.ScheduledPayoutCardId
            });
        }
        [HttpGet]
        public async Task<IActionResult> Performance(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.Statistics);
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

            var ratingDistribution = new int[5];
            foreach (var rating in allRatings)
            {
                var bucket = Math.Clamp((int)Math.Round(rating), 1, 5) - 1;
                ratingDistribution[bucket]++;
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
                ProductCount = products.Count,
                OrdersByStatus = distinctOrders
                    .GroupBy(o => o.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RatingDistribution = ratingDistribution
            });
        }

        [HttpGet]
        public async Task<IActionResult> PromoCodes(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.PromoCodes);
            if (business is null)
            {
                return Forbid();
            }

            var promoCodes = await _promoCodeService.GetForBusinessAsync(businessId);
            var businessDiscount = await _discountService.GetBusinessDiscountAsync(businessId);
            var categoryDiscounts = await _discountService.GetActiveCategoryDiscountsForBusinessAsync(businessId);
            var categories = await _categoryService.GetAllCategories();
            var personalDiscounts = await _userDiscountService.GetActiveDiscountsForBusinessAsync(businessId);
            var customersResult = await _businessService.GetCustomersStatsByPeriodAsync(businessId, business.CreatedAt, DateTime.UtcNow);
            var customers = customersResult.Succeeded
                ? customersResult.Data!.Stats
                    .Where(c => personalDiscounts.All(d => d.UserId != c.UserId))
                    .OrderBy(c => c.UserName)
                    .ToList()
                : new List<Marketplace.DAL.DTOs.CustomerStatsByPeriodDto>();

            return View(new BusinessPromoCodesPageViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                PromoCodes = promoCodes,
                BusinessDiscount = businessDiscount,
                CategoryDiscounts = categoryDiscounts,
                Categories = categories.ToList(),
                PersonalDiscounts = personalDiscounts,
                Customers = customers
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetBusinessDiscount(Guid businessId, decimal discountPercent, DateTimeOffset discountStartDate, DateTimeOffset discountEndDate)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.PromoCodes);
            if (business is null)
            {
                return Forbid();
            }

            var result = await _discountService.SetBusinessDiscount(businessId, discountPercent, discountStartDate.UtcDateTime, discountEndDate.UtcDateTime);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "BusinessDiscountSet",
                    $"Встановлено знижку {discountPercent}% на весь магазин «{business.StoreName}».",
                    actorUserId: _userManager.GetUserId(User));
            }
            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);

            return RedirectToAction(nameof(PromoCodes), new { businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBusinessDiscount(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.PromoCodes);
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
            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);

            return RedirectToAction(nameof(PromoCodes), new { businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromoCode(Guid businessId, string code, int percent, DateTimeOffset startDate, DateTimeOffset endDate, bool isSingleUse = false)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.PromoCodes);
            if (business is null)
            {
                return Forbid();
            }

            var result = await _promoCodeService.CreateAsync(businessId, code, percent, startDate.UtcDateTime, endDate.UtcDateTime, isSingleUse);
            if (result.Succeeded)
            {
                var usageNote = isSingleUse ? "одноразовий" : "багаторазовий";
                await _activityLog.LogBusinessActionAsync(businessId, "PromoCodeCreated",
                    $"Створено промокод «{code}» (знижка {percent}%, {usageNote}).",
                    actorUserId: _userManager.GetUserId(User), relatedEntityId: result.EntityId);
            }
            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);

            return RedirectToAction(nameof(PromoCodes), new { businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivatePromoCode(Guid businessId, Guid promoCodeId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.PromoCodes);
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
            TempData[result.Succeeded ? "Success" : "Error"] = this.L(result.Message);

            return RedirectToAction(nameof(PromoCodes), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> Growth(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.Statistics);
            if (business is null)
            {
                return Forbid();
            }

            return View(new BusinessGrowthViewModel { BusinessId = businessId, StoreName = business.StoreName });
        }

        [HttpGet]
        public async Task<IActionResult> Settings(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.EditBusinessProfile);
            if (business is null)
            {
                return Forbid();
            }

            return View(new BusinessSettingsViewModel
            {
                BusinessId = businessId,
                StoreName = business.StoreName,
                Description = business.Description,
                AdultStatus = (AdultContentStatus)business.AdultStatus
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(BusinessSettingsViewModel model)
        {
            var business = await GetAccessibleBusinessOrNullAsync(model.BusinessId, BusinessPermissions.EditBusinessProfile);
            if (business is null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var res = await _businessService.UpdateProfileAsync(model.BusinessId, model.StoreName, model.Description);

            if(!res.Succeeded)
            {
                TempData["Error"] = this.L(res.Message);
                return RedirectToAction(nameof(Settings), new { businessId = model.BusinessId });
            }

            TempData["Success"] = this.L("Дані магазину оновлено.");
            return RedirectToAction(nameof(Settings), new { businessId = model.BusinessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAdultStatus(Guid businessId)
        {
            var business = await GetAccessibleBusinessOrNullAsync(businessId, BusinessPermissions.EditBusinessProfile);
            if (business is null)
            {
                return Forbid();
            }

            var userId = _userManager.GetUserId(User);
            var result = await _businessService.RequestAdultStatusAsync(businessId, userId!);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? this.L("Заявку на розміщення 18+ контенту подано на розгляд адміністрації.")
                : result.Message;

            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "AdultStatusRequested",
                    "Магазин подав заявку на верифікацію 18+ контенту.", actorUserId: userId);
            }

            return RedirectToAction(nameof(Settings), new { businessId });
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

        private async Task<Business?> GetAccessibleBusinessOrNullAsync(Guid businessId, params BusinessMemberRole[] allowedRoles)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var business = await GetAccessibleBusinessOrNullAsync(businessId);
            if (business is null)
            {
                return null;
            }

            var userRole = await _businessAccessService.GetUserRoleAsync(businessId, userId);
            if (userRole is null || (allowedRoles.Length > 0 && !allowedRoles.Contains(userRole.Value)))
            {
                return null;
            }

            return business;
        }
    }
}