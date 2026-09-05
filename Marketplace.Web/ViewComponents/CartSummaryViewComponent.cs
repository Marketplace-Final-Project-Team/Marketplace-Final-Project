using System.Security.Claims;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.ViewComponents;

public class CartSummaryViewComponent : ViewComponent
{
    private readonly ICartService _cartService;

    public CartSummaryViewComponent(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return View(0);
        }

        var items = await _cartService.GetCartItems(userId);
        return View(items.Sum(item => item.Quantity));
    }
}
