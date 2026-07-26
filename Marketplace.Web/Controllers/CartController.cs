using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
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
            var userId = _userManager.GetUserId(User);

            var items = await _cartService.GetCartItems(userId!);

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid productId) 
        {
            var userId = _userManager.GetUserId(User);
            
            await _cartService.AddToCart(userId!, productId);

            var referer = Request.Headers["Referer"].ToString();

            if(!string.IsNullOrEmpty(referer)) 
            {
                return Redirect(referer);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid productId)
        {
            var userId = _userManager.GetUserId(User);

            await _cartService.RemoveFromCart(userId!, productId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncreaseQuantity(Guid productId)
        {
            var userId = _userManager.GetUserId(User);

            await _cartService.IncreaseQuantity(userId!, productId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecreaseQuantity(Guid productId)
        {
            var userId = _userManager.GetUserId(User);

            await _cartService.DecreaseQuantity(userId!, productId);

            return RedirectToAction("Index");
        }
    }
}
