using Marketplace.Web.Infrastructure;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Marketplace.Web.Controllers
{
    [Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        // GET /api/wishlist
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований");

            var wishlist = await _wishlistService.GetWishlistAsync(userId);
            return Ok(wishlist);
        }

        // POST /api/wishlist/add/{productId}
        [HttpPost("add/{productId}")]
        public async Task<IActionResult> AddToWishlist(Guid productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований");

            var result = await _wishlistService.AddToWishlistAsync(userId, productId);
            if (!result.Succeeded)
                return BadRequest(result.Message);

            return Ok(new { message = this.L(result.Message) });
        }

        // DELETE /api/wishlist/remove/{productId}
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(Guid productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Користувач не авторизований");

            var result = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            if (!result.Succeeded)
                return BadRequest(result.Message);

            return Ok(new { message = this.L(result.Message) });
        }
        
    }
}