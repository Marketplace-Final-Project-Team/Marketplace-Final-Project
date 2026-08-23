using System.Security.Claims;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Marketplace.Web.Infrastructure;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IProductService _productService;
    private readonly IGenericRepository<Address> _addressRepository;
    private readonly AppDbContext _context;

    public OrderController(
        IOrderService orderService,
        ICartService cartService,
        IProductService productService,
        IGenericRepository<Address> addressRepository,
        AppDbContext context)
    {
        _orderService = orderService;
        _cartService = cartService;
        _productService = productService;
        _addressRepository = addressRepository;
        _context = context;
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
            DeliveryMethodLabel = GetDeliveryChoice(order.Id)
        }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await BuildOrderDetails(id);
        return model is null ? NotFound() : View(model);
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
        await PopulateSavedAddresses(model);

        if (model.Items.Count == 0)
        {
            TempData["Error"] = "Додайте товари до кошика перед оформленням.";
            return RedirectToAction("Index", "Cart");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
        await PopulateCart(model);
        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Кошик порожній.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSavedAddresses(model);
            return View(model);
        }

        var userId = GetUserId();
        Address address;
        var isNewAddress = !model.IsUsingSavedAddress;

        if (isNewAddress)
        {
            address = new Address
            {
                UserId = userId,
                Country = model.Country!.Trim(),
                City = model.City!.Trim(),
                Street = model.Street!.Trim(),
                IsDefault = false
            };

            await _addressRepository.AddAsync(address);
            await _context.SaveChangesAsync();
        }
        else
        {
            var savedAddress = await _addressRepository.FirstOrDefaultAsync(
                a => a.Id == model.SelectedAddressId!.Value && a.UserId == userId);

            if (savedAddress is null)
            {
                ModelState.AddModelError(string.Empty, "Обрана адреса не знайдена.");
                await PopulateSavedAddresses(model);
                return View(model);
            }

            address = savedAddress;
        }

        var result = await _orderService.CreateOrder(userId!, new OrderCreateDto { AddressId = address.Id, SuccessUrl = model.SuccessUrl, CancelUrl = model.CancelUrl });

        if (!result.Succeeded)
        {
            if (isNewAddress)
            {
                _addressRepository.Remove(address);
                await _context.SaveChangesAsync();
            }

            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateSavedAddresses(model);
            return View(model);
        }
        if (Guid.TryParse(result.OrderId, out var createdOrderId))
        {
            RecordDeliveryChoice(createdOrderId, model);
            RecordPaymentChoice(createdOrderId, model);
        }

        // CreateOrder always opens a LiqPay session under the hood, but a buyer who chose
        // "pay on delivery" should never be pushed through it — the order already exists
        // (Status = Pending), so we just skip straight to its details page.
        if (result.PaymentData != null && model.PaymentMethod != PaymentMethods.CashOnDelivery)
        {
          return View("Payment", result.PaymentData);
        }

        TempData["Success"] = model.PaymentMethod == PaymentMethods.CashOnDelivery
            ? "Замовлення оформлено. Оплата — при отриманні."
            : "Замовлення успішно оформлено.";
        return RedirectToAction(nameof(Details), new { id = result.OrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeStatus(Guid orderId, OrderStatus status)
    {
        var result = await _orderService.ChangeOrderStatus(orderId, status);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    private void RecordDeliveryChoice(Guid orderId, OrderCreateViewModel model)
    {
        if (!CookieConsent.IsAccepted(Request.Cookies))
        {
            return;
        }

        var label = model.DeliveryCarrier == DeliveryCarriers.NovaPoshta
            ? $"Нова пошта — {model.NovaPoshtaBranchName}, {model.NovaPoshtaCityName}"
            : DeliveryCarriers.Label(model.DeliveryCarrier);

        Response.Cookies.Append(OrderDeliveryCookie.NameFor(orderId), label, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(OrderDeliveryCookie.Lifetime),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = false
        });
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

    private async Task PopulateSavedAddresses(OrderCreateViewModel model)
    {
        var userId = GetUserId();
        var addresses = await _addressRepository.FindAsync(a => a.UserId == userId);

        model.SavedAddresses = addresses
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressViewModel
            {
                Id = a.Id,
                Country = a.Country,
                City = a.City,
                Street = a.Street,
                IsDefault = a.IsDefault
            })
            .ToList();

        if (!model.SelectedAddressId.HasValue)
        {
            var defaultAddress = model.SavedAddresses.FirstOrDefault(a => a.IsDefault) ?? model.SavedAddresses.FirstOrDefault();
            if (defaultAddress is not null)
            {
                model.SelectedAddressId = defaultAddress.Id;
            }
        }
    }

    private async Task<OrderDetailsViewModel?> BuildOrderDetails(Guid id)
    {
        var order = await _orderService.GetUserOrder(GetUserId(), id);
        if (order is null)
        {
            return null;
        }

        return new OrderDetailsViewModel
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            ShippingCountry = order.ShippingCountry,
            ShippingCity = order.ShippingCity,
            ShippingStreet = order.ShippingStreet,
            DeliveryMethodLabel = GetDeliveryChoice(order.Id),
            PaymentMethodLabel = GetPaymentChoice(order.Id),
            Items = order.Items.Select(item => new OrderLineViewModel
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ImageUrl = item.Product?.MainImageUrl ?? string.Empty,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            }).ToList()
        };
    }
}
