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
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly AppDbContext _context;

        public BusinessController(
            IBusinessService businessService,
            IBusinessAccessService businessAccessService,
            IProductService productService,
            IOrderService orderService,
            IReviewService reviewService,
            UserManager<User> userManager,
            IEmailSender emailSender,
            AppDbContext context)
        {
            _businessService = businessService;
            _businessAccessService = businessAccessService;
            _productService = productService;
            _orderService = orderService;
            _reviewService = reviewService;
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
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
                }).ToList()
            });
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
    }
}