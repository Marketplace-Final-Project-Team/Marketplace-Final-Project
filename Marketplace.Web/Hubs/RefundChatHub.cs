using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Marketplace.Web.Hubs
{
    [Authorize]
    public class RefundChatHub : Hub
    {
        private readonly IRefundChatService _chatService;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly AppDbContext _context;

        public RefundChatHub(IRefundChatService chatService, IHubContext<NotificationHub> notificationHub, AppDbContext context)
        {
            _chatService = chatService;
            _notificationHub = notificationHub;
            _context = context;
        }

        private static string BuildGroupName(Guid requestId, bool isSellerChannel) =>
            $"{requestId}_{(isSellerChannel ? "seller" : "buyer")}";

        public async Task JoinChat(Guid requestId, bool isSellerChannel)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(requestId, isSellerChannel));
        }

        public async Task LeaveChat(Guid requestId, bool isSellerChannel)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildGroupName(requestId, isSellerChannel));
        }

        public async Task SendMessage(Guid requestId, bool isSellerChannel, string text)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

            var message = await _chatService.SaveMessageAsync(requestId, userId, isAdmin, isSellerChannel, text.Trim());
            if (message is null)
            {
                return;
            }

            await Clients.Group(BuildGroupName(requestId, isSellerChannel)).SendAsync("ReceiveMessage", new
            {
                senderId = message.SenderId,
                isFromAdmin = message.IsFromAdmin,
                text = message.Text,
                sentAt = message.SentAt
            });

           
            if (isAdmin && !isSellerChannel)
            {
                var request = await _context.PostSaleRequests.FirstOrDefaultAsync(r => r.Id == requestId);
                if (request is not null)
                {
                    await _notificationHub.Clients.User(request.BuyerId).SendAsync("ReceiveNotification", new
                    {
                        text = "Нове повідомлення від адміністратора щодо вашого повернення.",
                        type = "RefundChat"
                    });
                }
            }
        }
    }
}