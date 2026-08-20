using Marketplace.BLL.Authorization;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class DiscountsController : Controller
    {
        private readonly IDiscountService _discountService;
        private readonly IProductService _productService;
        private readonly IBusinessAccessService _businessAccessService;

        public DiscountsController(
            IDiscountService discountService,
            IProductService productService,
            IBusinessAccessService businessAccessService)
        {
            _discountService = discountService;
            _productService = productService;
            _businessAccessService = businessAccessService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // POST: /Discounts/Set
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Set(Guid productId, int percent, DateTime startDate, DateTime endDate)
        {
            var product = await _productService.GetByIdAsync(productId);
            if (product == null) return NotFound();

            var hasAccess = await _businessAccessService.HasAccessAsync(product.BusinessId, CurrentUserId, BusinessPermissions.ProductCrud);
            if (!hasAccess) return Forbid();

            try
            {
                await _discountService.SetDiscountAsync(productId, percent, startDate, endDate);
                TempData["Success"] = "Знижку встановлено.";
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Edit", "Products", new { id = productId });
        }

        // POST: /Discounts/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid productId)
        {
            var product = await _productService.GetByIdAsync(productId);
            if (product == null) return NotFound();

            var hasAccess = await _businessAccessService.HasAccessAsync(product.BusinessId, CurrentUserId, BusinessPermissions.ProductCrud);
            if (!hasAccess) return Forbid();

            await _discountService.RemoveDiscountAsync(productId);
            TempData["Success"] = "Знижку скасовано.";

            return RedirectToAction("Edit", "Products", new { id = productId });
        }
    }
}