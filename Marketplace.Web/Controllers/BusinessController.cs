using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Web.Models;
using Marketplace.Web.Models.Businesses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class BusinessController : Controller
    {
        private readonly IBusinessService _businessService;
        private readonly IBusinessAccessService _businessAccessService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public BusinessController(
            IBusinessService businessService,
            IBusinessAccessService businessAccessService,
            IProductService productService,
            IOrderService orderService,
            UserManager<User> userManager,
            IEmailSender emailSender)
        {
            _businessService = businessService;
            _businessAccessService = businessAccessService;
            _productService = productService;
            _orderService = orderService;
            _userManager = userManager;
            _emailSender = emailSender;
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

            return RedirectToAction("Index", "Home");
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
    }
}
