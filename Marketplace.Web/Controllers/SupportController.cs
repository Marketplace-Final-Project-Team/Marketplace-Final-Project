using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly IRefundChatService _chatService;
        private readonly AppDbContext _context;
        private readonly IBusinessAccessService _businessAccessService;
        private readonly ISupportTicketService _supportTicketService;

        public SupportController(
          IRefundChatService chatService,
          AppDbContext context,
          IBusinessAccessService businessAccessService,
          ISupportTicketService supportTicketService)   
        {
            _chatService = chatService;
            _context = context;
            _businessAccessService = businessAccessService;
            _supportTicketService = supportTicketService;  
        }


        [HttpGet]
        public async Task<IActionResult> RefundChat(Guid requestId, bool isSellerChannel = false)
        {
            var request = await _context.PostSaleRequests
                .Include(r => r.Order)
                .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (isSellerChannel)
            {
   
                var businessId = request.OrderItem?.Product?.BusinessId;
                var hasBusinessAccess = businessId.HasValue
                    && !string.IsNullOrEmpty(userId)
                    && await _businessAccessService.HasAccessAsync(businessId.Value, userId);

                if (!isAdmin && !hasBusinessAccess)
                {
                    return Forbid();
                }
            }
            else
            {
                
                if (!isAdmin && request.BuyerId != userId)
                {
                    return Forbid();
                }
            }

            await _chatService.MarkAsReadAsync(requestId, isAdmin, isSellerChannel);

            var messages = await _chatService.GetMessagesAsync(requestId, isSellerChannel);

            ViewBag.IsAdmin = isAdmin;
            ViewBag.RequestId = requestId;
            ViewBag.IsSellerChannel = isSellerChannel;
            ViewBag.ItemName = request.OrderItem?.ProductName ?? "Товар";
            ViewBag.OrderNumber = request.OrderId.ToString()[..8].ToUpperInvariant();
            var backBusinessId = request.OrderItem?.Product?.BusinessId;
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
                    new { businessId = backBusinessId.Value, orderId = request.OrderId })!;
                backLabel = "← До замовлення";
            }
            else
            {
                backUrl = Url.Action("Index", "Order")!;
                backLabel = "← До моїх замовлень";
            }

            ViewBag.BackUrl = backUrl;
            ViewBag.BackLabel = backLabel;
            return View(messages);
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
            var request = await _context.PostSaleRequests
                .FirstOrDefaultAsync(r => r.OrderItemId == orderItemId);

            if (request == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(RefundChat), new { requestId = request.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(string category, string message)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Потрібно увійти в акаунт." });
            }

            var result = await _supportTicketService.CreateTicketAsync(userId, category, message);
            return Json(new { success = result.Succeeded, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> TicketChat(Guid ticketId)
        {
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket is null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && ticket.UserId != userId)
            {
                return Forbid();
            }

            if (isAdmin)
            {
                await _supportTicketService.AdminOpenChatAsync(ticketId);
            }

            await _supportTicketService.MarkAsReadAsync(ticketId, isAdmin);
            var messages = await _supportTicketService.GetMessagesAsync(ticketId);

            ViewBag.IsAdmin = isAdmin;
            ViewBag.TicketId = ticketId;
            ViewBag.Category = ticket.Category;

            return View(messages);
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
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket is null)
            {
                return Json(new { status = "NotFound" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ticket.UserId != userId)
            {
                return Forbid();
            }

            return Json(new { status = ticket.Status.ToString() });
        }

    }
}