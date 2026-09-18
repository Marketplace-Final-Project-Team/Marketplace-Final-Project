using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Marketplace.Web.Hubs
{
    [Authorize]
    public class RefundChatHub : Hub
    {
        private readonly IRefundChatService _chatService;

        public RefundChatHub(IRefundChatService chatService)
        {
            _chatService = chatService;
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
        }
    }
}