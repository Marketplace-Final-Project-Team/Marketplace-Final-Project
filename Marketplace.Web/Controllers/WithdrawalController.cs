using Marketplace.Web.Infrastructure;
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

        
        private async Task<(string? UserId, bool HasAccess)> CheckOwnerAccessAsync(Guid businessId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return (null, false);
            }

            var hasAccess = await _businessAccessService.HasAccessAsync(businessId, userId, BusinessMemberRole.Owner);
            return (userId, hasAccess);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid businessId, decimal amount, Guid? savedCardId, string? cardNumber, string? recipientName, bool rememberCard = false)
        {
            if (amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }
            var business = await _businessService.GetByIdAsync(businessId);
            if (business == null)
            {
                return NotFound();
            }

            var (userId, hasAccess) = await CheckOwnerAccessAsync(businessId);
            if (userId == null)
            {
                return Unauthorized();
            }
            if (!hasAccess)
            {
                return Forbid();
            }

            var result = await _withdrawalService.Create(businessId, amount, savedCardId, cardNumber, recipientName, rememberCard);
            if (!result.Succeeded)
            {
                return BadRequest(result.Message);
            }
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetSavedCards(Guid businessId)
        {
            var (userId, hasAccess) = await CheckOwnerAccessAsync(businessId);
            if (userId == null)
            {
                return Unauthorized();
            }
            if (!hasAccess)
            {
                return Forbid();
            }

            var cards = await _withdrawalService.GetSavedCardsAsync(businessId);
            return Ok(cards);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSavedCard(Guid businessId, Guid cardId)
        {
            var (userId, hasAccess) = await CheckOwnerAccessAsync(businessId);
            if (userId == null)
            {
                return Unauthorized();
            }
            if (!hasAccess)
            {
                return Forbid();
            }

            var result = await _withdrawalService.DeleteSavedCardAsync(businessId, cardId);
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

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Withdrawals(WithdrawalStatus? status)
        {
            var withdrawals = await _withdrawalService.GetAllWithdrawalsAsync(status);
            return View(new Marketplace.Web.Models.WithdrawalsAdminViewModel
            {
                StatusFilter = status,
                Withdrawals = withdrawals
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveWithdrawal(Guid requestId)
        {
            var currentAdminId = _userManager.GetUserId(User);

            var result = await _withdrawalService.ApproveWithdrawalRequestAsync(requestId, currentAdminId!);

            if (result.Succeeded)
            {
                TempData["Success"] = this.L("Виплату успішно здійснено.");
            }
            else
            {
                TempData["Error"] = this.L(result.Message);
            }

            return RedirectToAction("Withdrawals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectWithdrawal(Guid requestId, string reason)
        {
            var currentAdminId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = this.L("Вкажіть причину відхилення.");
                return RedirectToAction("Withdrawals");
            }

            var result = await _withdrawalService.RejectWithdrawalRequestAsync(requestId, reason.Trim(), currentAdminId!);

            if (result.Succeeded)
            {
                TempData["Success"] = this.L("Запит відхилено.");
            }
            else
            {
                TempData["Error"] = this.L(result.Message);
            }

            return RedirectToAction("Withdrawals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetScheduledPayoutSettings(Guid businessId, bool enabled, int? dayOfMonth, decimal? amount, Guid? savedCardId)
        {
            var (userId, hasAccess) = await CheckOwnerAccessAsync(businessId);
            if (userId == null)
            {
                return Unauthorized();
            }
            if (!hasAccess)
            {
                return Forbid();
            }

            var result = await _withdrawalService.SetScheduledPayoutSettingsAsync(businessId, enabled, dayOfMonth, amount, savedCardId);
            if (!result.Succeeded)
            {
                return BadRequest(result.Message);
            }

            return Ok();
        }
    }
}