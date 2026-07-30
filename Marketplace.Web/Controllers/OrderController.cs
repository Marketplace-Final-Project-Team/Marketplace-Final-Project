using System.Security.Claims;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IGenericRepository<Address> _addressRepository;
    private readonly AppDbContext _context;

    public OrderController(
        IOrderService orderService,
        ICartService cartService,
        IGenericRepository<Address> addressRepository,
        AppDbContext context)
    {
        _orderService = orderService;
        _cartService = cartService;
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
            ShippingAddress = $"{order.ShippingCity}, {order.ShippingStreet}"
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
            return View(model);
        }

        var userId = GetUserId();
        var address = new Address
        {
            UserId = userId,
            Country = model.Country.Trim(),
            City = model.City.Trim(),
            Street = model.Street.Trim(),
            IsDefault = true
        };

        await _addressRepository.AddAsync(address);
        await _context.SaveChangesAsync();

        var result = await _orderService.CreateOrder(
            userId,
            new OrderCreateDto { AddressId = address.Id });

        if (!result.Succeeded)
        {
            _addressRepository.Remove(address);
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = "Замовлення успішно оформлено.";
        return RedirectToAction(nameof(Details), new { id = result.EntityId });
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

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Авторизованого користувача не знайдено.");
    }

    private async Task PopulateCart(OrderCreateViewModel model)
    {
        model.Items = await _cartService.GetCartItems(GetUserId());
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
