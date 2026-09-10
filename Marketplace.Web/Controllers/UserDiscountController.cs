using Marketplace.BLL.Common;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    public class UserDiscountController : Controller
    {
        private readonly IBusinessAccessService _businessAccessService;
        public readonly IUserDiscountService _userDiscountService;
        public readonly IBusinessService _businessService;
        public UserDiscountController(IBusinessAccessService businessAccessService, IUserDiscountService userDiscountService,IBusinessService businessService)
        {
            _businessAccessService = businessAccessService;
            _userDiscountService = userDiscountService;
            _businessService = businessService;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserDiscount(string userId,Guid businessId, decimal percent, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine(percent);
            var business = await _businessService.GetByIdAsync(businessId);
            var hasAcces = await _businessAccessService.HasAccessAsync(businessId, business.OwnerId, BusinessPermissions.ProductCrud);
            if (!hasAcces) {
                return Forbid();
            }
            var result = await _userDiscountService.SetUserDiscount(userId, businessId, percent, startDate, endDate);
            if (!result.Succeeded)
            {
                Console.WriteLine(result.Message);
                return BadRequest(result.Message);
            }
            
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserDiscount(string userId, Guid businessId)
        {
            var business = await _businessService.GetByIdAsync(businessId);
            var hasAcces = await _businessAccessService.HasAccessAsync(businessId, business.OwnerId, BusinessPermissions.ProductCrud);
            if (!hasAcces)
            {
                return Forbid();
            }
            var result = await _userDiscountService.RemoveUserDiscount(userId, businessId);
            if (!result.Succeeded)
            {
                return BadRequest(result.Message);
            }
            return Ok();
        }
        [HttpGet]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetUserDiscount(string userId, Guid businessId)
        {
            var business = await _businessService.GetByIdAsync(businessId);
            var hasAcces = await _businessAccessService.HasAccessAsync(businessId, business.OwnerId, BusinessPermissions.Statistics);
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
