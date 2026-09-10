using Marketplace.BLL.Common;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class WithdrawalController : Controller
    {
        private readonly IBusinessService _businessService;
        private readonly IBusinessAccessService _businessAccessService;
        private readonly IWithdrawalService _withdrawalService;
        private readonly UserManager<User> _userManager;
        public WithdrawalController(IBusinessService businessService, IBusinessAccessService businessAccessService, IWithdrawalService withdrawalService, UserManager<User> userManager)
        {
            _businessService = businessService;
            _businessAccessService = businessAccessService;
            _withdrawalService = withdrawalService;
            _userManager = userManager;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid businessId, decimal amount, string cardNumber, string recipientName)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(recipientName))
            {
                return BadRequest("Card number and recipient name are required.");
            }
            if (amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }
            Console.WriteLine("VSEEEE OK ");
            var business = await _businessService.GetByIdAsync(businessId);
            if (business == null)
            {
                return NotFound();
            }
            var userId = _userManager.GetUserId(User);
            if (userId == null) {
                return Unauthorized();
            }
            var hasAcces = await _businessAccessService.HasAccessAsync(businessId, userId, BusinessMemberRole.Owner);
            if (!hasAcces)
            {
                return Forbid();
            }
            var result = await _withdrawalService.Create(businessId, amount, cardNumber, recipientName);
            if (!result.Succeeded)
            {
                return BadRequest(result.Message);
            }   
            return Ok(); 
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllWithdrawals(WithdrawalStatus? status)
        {
            var withdrawals = await _withdrawalService.GetAllWithdrawalsAsync(status);
            return Ok(withdrawals);
        }
    }
}
