using System.Security.Claims;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notificationService.GetUnreadCountForBuyerAsync(GetUserId());
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var items = await _notificationService.GetRecentNotificationsAsync(GetUserId());

            var result = items.Select(i => new
            {
                type = i.Type,
                text = i.Text,
                time = i.Time,
                url = BuildUrl(i.Type, i.LinkId)
            });

            return Json(result);
        }

        private string BuildUrl(string type, Guid linkId)
        {
            return type switch
            {
                "ReturnStatus" => Url.Action("Details", "Order", new { id = linkId }) ?? "#",
                "RefundChat" => Url.Action("RefundChat", "Support", new { requestId = linkId, isSellerChannel = false }) ?? "#",
                "SupportTicket" => Url.Action("TicketChat", "Support", new { ticketId = linkId }) ?? "#",
                _ => "#"
            };
        }
    }
}