using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly UserManager<User> _userManager;

    public CartController(ICartService cartService, UserManager<User> userManager)
    {
        _cartService = cartService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _cartService.GetCartItems(GetUserId());
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productId, string? returnUrl = null)
    {
        var result = await _cartService.AddToCart(GetUserId(), productId);
        SetResultMessage(result.Succeeded, result.Message, "Товар додано до кошика.");

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid productId)
    {
        var result = await _cartService.RemoveFromCart(GetUserId(), productId);
        SetResultMessage(result.Succeeded, result.Message, "Товар видалено з кошика.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IncreaseQuantity(Guid productId)
    {
        var result = await _cartService.IncreaseQuantity(GetUserId(), productId);
        SetResultMessage(result.Succeeded, result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DecreaseQuantity(Guid productId)
    {
        var result = await _cartService.DecreaseQuantity(GetUserId(), productId);
        SetResultMessage(result.Succeeded, result.Message);
        return RedirectToAction(nameof(Index));
    }

    private string GetUserId()
    {
        return _userManager.GetUserId(User)
            ?? throw new InvalidOperationException("Авторизованого користувача не знайдено.");
    }

    private void SetResultMessage(bool succeeded, string errorMessage, string? successMessage = null)
    {
        if (succeeded && !string.IsNullOrWhiteSpace(successMessage))
        {
            TempData["Success"] = successMessage;
        }
        else if (!succeeded)
        {
            TempData["Error"] = errorMessage;
        }
    }
}
