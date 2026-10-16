using System.Security.Claims;
using Marketplace.BLL.Common;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class UserDiscountController : Controller
    {
        private readonly IBusinessAccessService _businessAccessService;
        private readonly IUserDiscountService _userDiscountService;

        public UserDiscountController(IBusinessAccessService businessAccessService, IUserDiscountService userDiscountService)
        {
            _businessAccessService = businessAccessService;
            _userDiscountService = userDiscountService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserDiscount(string userId, Guid businessId, decimal percent, DateTimeOffset startDate, DateTimeOffset endDate, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var hasAcces = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.ProductCrud);
            if (!hasAcces)
            {
                return Forbid();
            }
            var result = await _userDiscountService.SetUserDiscount(userId, businessId, percent, startDate.UtcDateTime, endDate.UtcDateTime);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("PromoCodes", "Business", new { businessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserDiscount(string userId, Guid businessId, string? returnUrl = null)
        {
            var hasAcces = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.ProductCrud);
            if (!hasAcces)
            {
                return Forbid();
            }
            var result = await _userDiscountService.RemoveUserDiscount(userId, businessId);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("PromoCodes", "Business", new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDiscount(string userId, Guid businessId)
        {
            var hasAcces = await _businessAccessService.HasAccessAsync(businessId, CurrentUserId, BusinessPermissions.Statistics);
            if (!hasAcces)
            {
                return Forbid();
            }
            var userDiscount = await _userDiscountService.GetUserDiscountAsync(userId, businessId);
            if (userDiscount == null)
            {
                return NotFound();
            }
            return Json(userDiscount);
        }
    }
}
