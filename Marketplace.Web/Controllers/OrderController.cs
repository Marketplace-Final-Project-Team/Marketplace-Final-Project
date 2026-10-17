using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Services;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Web.Infrastructure;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketplace.Web.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IProductService _productService;
    private readonly IPostSaleService _postSaleService;
    private readonly IPromoCodeService _promoCodeService;
    private readonly IActivityLogService _activityLog;
    private readonly INotificationService _notificationService;
    private readonly IAddressService _addressService;

    public OrderController(
        IOrderService orderService,
        ICartService cartService,
        IProductService productService,
        IPostSaleService postSaleService,
        IPromoCodeService promoCodeService,
        IActivityLogService activityLog,
        INotificationService notificationService,
        IAddressService addressService)
    {
        _orderService = orderService;
        _cartService = cartService;
        _productService = productService;
        _postSaleService = postSaleService;
        _promoCodeService = promoCodeService;
        _activityLog = activityLog;
        _notificationService = notificationService;
        _addressService = addressService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = await _orderService.GetUserOrders(GetUserId());
        return View(orders.Select(order => new OrderListItemViewModel
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            ItemsCount = order.Items.Sum(item => item.Quantity),
            ShippingAddress = $"{order.ShippingCity}, {order.ShippingStreet}",
            DeliveryCarrierLabel = GetDeliveryCarrierLabel(order),
            DeliveryMethodLabel = GetDeliveryLabel(order)
        }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await BuildOrderDetails(id);
        if (model is null)
        {
            return NotFound();
        }

        await _notificationService.MarkOrderViewedAsync(GetUserId(), id);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delivery(Guid id)
    {
        var model = await BuildOrderDetails(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new OrderCreateViewModel();
        await PopulateCart(model);
        await PopulateAddresses(model);

        if (model.Items.Count == 0)
        {
            TempData["Error"] = this.L("Додайте товари до кошика перед оформленням.");
            return RedirectToAction("Index", "Cart");
        }

        var defaultAddress = model.SavedAddresses.FirstOrDefault(a => a.IsDefault);
        if (defaultAddress != null)
        {
            model.DeliveryCity = defaultAddress.City;
            model.DeliveryStreet = defaultAddress.Street;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
        await PopulateCart(model);
        await PopulateAddresses(model);

        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, this.L("Кошик порожній."));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetUserId();
        var scheme = Request.Scheme;
        var host = Request.Host;

        var result = await _orderService.CreateOrder(userId!, new OrderCreateDto
        {
            RecipientLastName = model.RecipientLastName,
            RecipientFirstName = model.RecipientFirstName,
            RecipientPhone = model.RecipientPhone,
            DeliveryCarrier = model.DeliveryCarrier,
            PaymentMethod = model.PaymentMethod,
            ShippingCity = model.DeliveryCarrier == DeliveryCarriers.NovaPoshta
                ? model.NovaPoshtaCityName! : model.DeliveryCity!,
            ShippingStreet = model.DeliveryCarrier == DeliveryCarriers.NovaPoshta
                ? model.NovaPoshtaBranchName! : model.DeliveryStreet!,
            NovaPoshtaCityRef = model.DeliveryCarrier == DeliveryCarriers.NovaPoshta ? model.NovaPoshtaCityRef : null,
            NovaPoshtaBranchRef = model.DeliveryCarrier == DeliveryCarriers.NovaPoshta ? model.NovaPoshtaBranchRef : null,
            PromoCode = model.PromoCode,
            SuccessUrl = $"{scheme}://{host}/Order",
            CancelUrl = $"{scheme}://{host}/Order"
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, this.L(result.Message));
            return View(model);
        }

        if (Guid.TryParse(result.OrderId, out var createdOrderId))
        {
            RecordDeliveryChoice(createdOrderId, model);
            RecordPaymentChoice(createdOrderId, model);
            await _activityLog.LogUserActionAsync(userId!, "OrderCreated", $"Створено замовлення на суму {model.Total:N2} ₴.", relatedEntityId: createdOrderId);
        }

        if (result.PaymentData != null && model.PaymentMethod != PaymentMethods.CashOnDelivery)
        {
            return View("Payment", result.PaymentData);
        }

        TempData["Success"] = model.PaymentMethod == PaymentMethods.CashOnDelivery
            ? this.L("Замовлення оформлено. Оплата - при отриманні.")
            : this.L("Замовлення успішно оформлено.");
        return RedirectToAction(nameof(Details), new { id = result.OrderId });
    }

    [HttpGet]
    public async Task<IActionResult> ValidatePromoCode(string code)
    {
        var preview = await _promoCodeService.PreviewAsync(GetUserId(), code);
        return Json(preview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReturn(Guid orderId, Guid orderItemId, string reason, string comment, List<IFormFile> photos)
    {
        var result = await _postSaleService.CreateRequestAsync(GetUserId(), orderItemId, reason?.Trim() ?? string.Empty, comment, photos);

        if (!result.Succeeded)
        {
            return Json(new { success = false, message = this.L(result.Message) });
        }

        await _activityLog.LogUserActionAsync(GetUserId(), "ReturnRequested", $"Подано заявку на повернення товару в замовленні (причина: {reason?.Trim()}).", relatedEntityId: orderItemId);

        var requestId = result.EntityId;

        return Json(new { success = true, requestId = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeStatus(Guid orderId, OrderStatus status)
    {
        var result = await _orderService.ChangeOrderStatus(orderId, status);
        if (!result.Succeeded)
        {
            TempData["Error"] = this.L(result.Message);
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    private void RecordDeliveryChoice(Guid orderId, OrderCreateViewModel model)
    {
        if (!CookieConsent.IsAccepted(Request.Cookies))
        {
            return;
        }

        var label = model.DeliveryCarrier switch
        {
            DeliveryCarriers.NovaPoshta => $"Нова пошта - {model.NovaPoshtaBranchName}, {model.NovaPoshtaCityName}",
            DeliveryCarriers.Ukrposhta or DeliveryCarriers.Meest =>
                $"{DeliveryCarriers.Label(model.DeliveryCarrier)} - {model.DeliveryCity}, {model.DeliveryStreet}",
            _ => DeliveryCarriers.Label(model.DeliveryCarrier)
        };

        Response.Cookies.Append(OrderDeliveryCookie.NameFor(orderId), label, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(OrderDeliveryCookie.Lifetime),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = false
        });
    }

    private string? GetDeliveryCarrierLabel(Order order)
    {
        return string.IsNullOrWhiteSpace(order.DeliveryCarrier)
            ? GetDeliveryChoice(order.Id)?.Split(" - ", 2)[0]
            : DeliveryCarriers.Label(order.DeliveryCarrier);
    }

    private string? GetDeliveryLabel(Order order)
    {
        return string.IsNullOrWhiteSpace(order.DeliveryCarrier)
            ? GetDeliveryChoice(order.Id)
            : $"{DeliveryCarriers.Label(order.DeliveryCarrier)} - {order.ShippingCity}, {order.ShippingStreet}";
    }

    private string? GetDeliveryChoice(Guid orderId)
    {
        return Request.Cookies.TryGetValue(OrderDeliveryCookie.NameFor(orderId), out var label) && !string.IsNullOrWhiteSpace(label)
            ? label
            : null;
    }

    private void RecordPaymentChoice(Guid orderId, OrderCreateViewModel model)
    {
        if (!CookieConsent.IsAccepted(Request.Cookies))
        {
            return;
        }

        Response.Cookies.Append(OrderPaymentCookie.NameFor(orderId), PaymentMethods.Label(model.PaymentMethod), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(OrderPaymentCookie.Lifetime),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = false
        });
    }

    private string? GetPaymentChoice(Guid orderId)
    {
        return Request.Cookies.TryGetValue(OrderPaymentCookie.NameFor(orderId), out var label) && !string.IsNullOrWhiteSpace(label)
            ? label
            : null;
    }

    private static PostSaleRequestViewModel? ToViewModel(PostSaleRequestDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new PostSaleRequestViewModel
        {
            Reason = dto.Reason,
            Comment = dto.Comment ?? string.Empty,
            PhotoUrls = dto.PhotoUrls,
            SubmittedAtUtc = dto.CreatedAt,
            Status = dto.Status,
            SellerResponse = dto.SellerResponse
        };
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Авторизованого користувача не знайдено.");
    }

    private async Task PopulateCart(OrderCreateViewModel model)
    {
        model.Items = await _cartService.GetCartItems(GetUserId());
        model.SellerGroups = await CartGrouping.GroupBySellerAsync(model.Items, _productService);
    }

    private async Task<OrderDetailsViewModel?> BuildOrderDetails(Guid id)
    {
        var order = await _orderService.GetUserOrder(GetUserId(), id);
        if (order is null)
        {
            return null;
        }

        var postSaleRequests = await _postSaleService.GetRequestsForOrderAsync(order.Id);

        return new OrderDetailsViewModel
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            RecipientLastName = order.RecipientLastName,
            RecipientFirstName = order.RecipientFirstName,
            RecipientPhone = order.RecipientPhone,
            ShippingCountry = order.ShippingCountry,
            ShippingCity = order.ShippingCity,
            ShippingStreet = order.ShippingStreet,
            DeliveryCarrierLabel = GetDeliveryCarrierLabel(order),
            DeliveryMethodLabel = GetDeliveryLabel(order),
            PaymentMethodLabel = string.IsNullOrWhiteSpace(order.PaymentMethod)
                ? GetPaymentChoice(order.Id) : PaymentMethods.Label(order.PaymentMethod),
            Items = order.Items.Select(item => new OrderLineViewModel
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ImageUrl = item.Product?.MainImageUrl ?? string.Empty,
                VariantSize = item.VariantSize,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                ReturnRequest = ToViewModel(postSaleRequests.FirstOrDefault(r => r.OrderItemId == item.Id))
            }).ToList()
        };
    }

    private async Task PopulateAddresses(OrderCreateViewModel model)
    {
        var addresses = await _addressService.GetUserAddressesAsync(GetUserId());
        model.SavedAddresses = addresses.Select(a => new AddressViewModel
        {
            Id = a.Id,
            Country = a.Country,
            City = a.City,
            Street = a.Street,
            IsDefault = a.IsDefault
        }).ToList();
    }
}
