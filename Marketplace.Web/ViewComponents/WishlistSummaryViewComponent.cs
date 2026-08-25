using System.Security.Claims;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.ViewComponents;

public class WishlistSummaryViewComponent : ViewComponent
{
    private readonly IWishlistService _wishlistService;

    public WishlistSummaryViewComponent(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Content(string.Empty);
        }

        var wishlist = await _wishlistService.GetWishlistAsync(userId);
        return View(wishlist.TotalItems);
    }
}
