using Marketplace.Web.Infrastructure;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly IRefundChatService _chatService;
        private readonly IBusinessAccessService _businessAccessService;
        private readonly ISupportTicketService _supportTicketService;

        public SupportController(
          IRefundChatService chatService,
          IBusinessAccessService businessAccessService,
          ISupportTicketService supportTicketService)   
        {
            _chatService = chatService;
            _businessAccessService = businessAccessService;
            _supportTicketService = supportTicketService;  
        }


        [HttpGet]
        public async Task<IActionResult> RefundChat(Guid requestId, bool isSellerChannel = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var page = await _chatService.GetChatPageAsync(requestId, userId, isAdmin, isSellerChannel);

            switch (page.Status)
            {
                case AccessStatus.NotFound: return NotFound();
                case AccessStatus.Forbidden: return Forbid();
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.RequestId = requestId;
            ViewBag.IsSellerChannel = isSellerChannel;
            ViewBag.ItemName = page.ProductName ?? this.L("Товар");
            ViewBag.OrderNumber = page.OrderId.ToString()[..8].ToUpperInvariant();
            var backBusinessId = page.BusinessId;
            string backUrl;
            string backLabel;

            if (isAdmin)
            {
                backUrl = Url.Action("Jobs", "Admin")!;
                backLabel = "← До задач";
            }
            else if (isSellerChannel && backBusinessId.HasValue)
            {
                backUrl = Url.Action("OrderDetails", "Business",
                    new { businessId = backBusinessId.Value, orderId = page.OrderId })!;
                backLabel = "← До замовлення";
            }
            else
            {
                backUrl = Url.Action("Index", "Order")!;
                backLabel = "← До моїх замовлень";
            }

            ViewBag.BackUrl = backUrl;
            ViewBag.BackLabel = backLabel;
            return View(page.Messages);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminRefundsInbox()
        {
            var threads = await _chatService.GetOpenThreadsForAdminAsync();
            return View(threads);
        }

        [HttpGet]
        public async Task<IActionResult> RefundChatByOrderItem(Guid orderItemId)
        {
            var requestId = await _chatService.GetRequestIdByOrderItemAsync(orderItemId);

            if (requestId == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(RefundChat), new { requestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(string category, string message)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = this.L("Потрібно увійти в акаунт.") });
            }

            var result = await _supportTicketService.CreateTicketAsync(userId, category, message);
            return Json(new { success = result.Succeeded, message = this.L(result.Message) });
        }

        [HttpGet]
        public async Task<IActionResult> TicketChat(Guid ticketId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var page = await _supportTicketService.GetTicketChatAsync(ticketId, userId, isAdmin);

            switch (page.Status)
            {
                case AccessStatus.NotFound: return NotFound();
                case AccessStatus.Forbidden: return Forbid();
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.TicketId = ticketId;
            ViewBag.Category = page.Category;

            return View(page.Messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CloseTicket(Guid ticketId)
        {
            await _supportTicketService.CloseTicketAsync(ticketId);
            return RedirectToAction("Jobs", "Admin");
        }

        [HttpGet]
        public async Task<IActionResult> GetTicketStatus(Guid ticketId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _supportTicketService.GetTicketStatusAsync(ticketId, userId);

            return result.Access switch
            {
                AccessStatus.NotFound => Json(new { status = "NotFound" }),
                AccessStatus.Forbidden => Forbid(),
                _ => Json(new { status = result.TicketStatus!.ToString() })
            };
        }

    }
}