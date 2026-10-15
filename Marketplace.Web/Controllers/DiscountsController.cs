using Marketplace.BLL.Common;
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
        private readonly IActivityLogService _activityLog;
        private readonly IBusinessService _businessService;

        private readonly ICategoryService _categoryService;

        public DiscountsController(
            IDiscountService discountService,
            IProductService productService,
            IBusinessAccessService businessAccessService,
            IBusinessService businessService,
            IActivityLogService activityLog,ICategoryService categoryService)
        {
            _discountService = discountService;
            _productService = productService;
            _businessAccessService = businessAccessService;
            _activityLog = activityLog;
            _categoryService = categoryService;
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
            startDate = startDate.Date <= DateTime.Today ? DateTime.UtcNow : startDate.Date.ToUniversalTime();
            endDate = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            try
            {
                await _discountService.SetDiscountAsync(productId, percent, startDate, endDate);
                await _activityLog.LogBusinessActionAsync(product.BusinessId, "DiscountSet",
                    $"Встановлено знижку {percent}% на товар «{product.Name}».",
                    actorUserId: CurrentUserId, relatedEntityId: productId);
                TempData["Success"] = "Знижку встановлено.";
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index", "Products");
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
            await _activityLog.LogBusinessActionAsync(product.BusinessId, "DiscountRemoved",
                $"Знижку на товар «{product.Name}» скасовано.",
                actorUserId: CurrentUserId, relatedEntityId: productId);
            TempData["Success"] = "Знижку скасовано.";

            return RedirectToAction("Index", "Products");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetBusinessDiscount(Guid businessId, decimal discountPercent, DateTime discountStartDate, DateTime discountEndDate)
        {
            
            var business = await _businessService.GetByIdAsync(businessId);
            if (business == null) return NotFound();

            var hasAccess = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.ProductCrud);
            if (!hasAccess) return Forbid();
            var result = await _discountService.SetBusinessDiscount(businessId, discountPercent, discountStartDate, discountEndDate);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "BusinessDiscountSet",
                    $"Адміністратор встановив знижку {discountPercent}% на весь бізнес «{business.StoreName}».",
                    actorUserId: CurrentUserId);
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Index","Business");

        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBusinessDiscount(Guid businessId)
        {
            
            var business = await _businessService.GetByIdAsync(businessId);
            if (business == null) return NotFound();

            var hasAccess = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.ProductCrud);
            if (!hasAccess) return Forbid();
            var result = await _discountService.RemoveBusinessDiscount(businessId);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "BusinessDiscountRemoved",
                    $"Адміністратор скасував знижку на весь бізнес «{business.StoreName}».",
                    actorUserId: CurrentUserId);
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Index", "Business");

        }
                                                               
        [HttpGet]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetBusinessDiscount(Guid businessId)
        {
            var business = await _businessService.GetByIdAsync(businessId);
            if (business == null) return NotFound();
            var hasAccess = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.Statistics);
            if (!hasAccess) return Forbid();
            var discount = await _discountService.GetBusinessDiscount(businessId);
            return Json(discount);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCategoryDiscount(Guid categoryId, Guid businessId, decimal discountPercent, DateTime discountStartDate, DateTime discountEndDate)
        {
            Console.WriteLine($"CategoryId: {categoryId}");
            
            var category = await _categoryService.GetByIdAsync(categoryId);
            //Console.WriteLine($"CategoryId: {categoryId}");
            //Console.WriteLine($"Category: {category?.Id} - {category?.Name}");
            if (category == null) return NotFound();

            var hasAccess = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.ProductCrud);
            if (!hasAccess) return Forbid();
            var result = await _discountService.SetCategoryDiscount(categoryId, businessId, discountPercent, discountStartDate, discountEndDate);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "CategoryDiscountSet",
                    $"Адміністратор встановив знижку {discountPercent}% на категорію «{category.Name}».",
                    actorUserId: CurrentUserId);
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("PromoCodes", "Business", new { businessId });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCategoryDiscount(Guid categoryId, Guid businessId)
        {

            var category = await _categoryService.GetByIdAsync(categoryId);
            if (category == null) return NotFound();

            var hasAccess = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.ProductCrud);
            if (!hasAccess) return Forbid();
            var result = await _discountService.RemoveCategoryDiscount(categoryId, businessId);
            if (result.Succeeded)
            {
                await _activityLog.LogBusinessActionAsync(businessId, "CategoryDiscountRemoved",
                    $"Адміністратор скасував знижку на категорію «{category.Name}».",
                    actorUserId: CurrentUserId);
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("PromoCodes", "Business", new { businessId });

        }

        [HttpGet]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCategoryDiscount(Guid categoryId, Guid businessId)
        {
            var category = await _categoryService.GetByIdAsync(categoryId);
            if (category == null) return NotFound();
            var hasAccess = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.Statistics);
            if (!hasAccess) return Forbid();
            var discount = await _discountService.GetCategoryDiscount(categoryId, businessId);
            return Json(discount);
        }
    }
}