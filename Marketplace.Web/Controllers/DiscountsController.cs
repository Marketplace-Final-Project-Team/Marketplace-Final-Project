using Marketplace.BLL.Authorization;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Services;
using Marketplace.Domain.Entities;
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
        private readonly IBusinessService _businessService;

        public DiscountsController(
            IDiscountService discountService,
            IProductService productService,
            IBusinessAccessService businessAccessService,
            IBusinessService businessService)
        {
            _discountService = discountService;
            _productService = productService;
            _businessAccessService = businessAccessService;
            _businessService = businessService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // POST: /Discounts/Set
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Set(Guid productId, decimal percent, DateTime startDate, DateTime endDate)
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
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetBusinessDiscount(Guid businessId, decimal discountPercent, DateTime discountStartDate, DateTime discountEndDate)
        {
            var business = await _businessService.GetByIdAsync(businessId);
            if (business == null) return NotFound();
            var result = await _discountService.SetBusinessDiscount(businessId, discountPercent, discountStartDate, discountEndDate);
            return RedirectToAction("Businesses");
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBusinessDiscount(Guid businessId)
        {
            var business = await _businessService.GetByIdAsync(businessId);
            if (business == null) return NotFound();
            var result = await _discountService.RemoveBusinessDiscount(businessId);
            return RedirectToAction("Businesses");
        }
        [Authorize(Roles = "Admin")]                                                         
        [HttpGet]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetBusinessDiscount(Guid businessId)
        {
            var business = await _businessService.GetByIdAsync(businessId);
            if (business == null) return NotFound();
            var discount = await _discountService.GetBusinessDiscount(businessId);
            return Json(discount);
        }
    }
}