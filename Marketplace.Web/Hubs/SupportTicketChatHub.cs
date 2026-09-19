using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Marketplace.Web.Hubs
{
    [Authorize]
    public class SupportTicketChatHub : Hub
    {
        private readonly ISupportTicketService _ticketService;

        public SupportTicketChatHub(ISupportTicketService ticketService)
        {
            _ticketService = ticketService;
        }

        public async Task JoinChat(Guid ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ticketId.ToString());
        }

        public async Task LeaveChat(Guid ticketId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ticketId.ToString());
        }

        public async Task SendMessage(Guid ticketId, string text)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

            var message = await _ticketService.SaveMessageAsync(ticketId, userId, isAdmin, text.Trim());
            if (message is null)
            {
                return;
            }

            await Clients.Group(ticketId.ToString()).SendAsync("ReceiveMessage", new
            {
                senderId = message.SenderId,
                isFromAdmin = message.IsFromAdmin,
                text = message.Text,
                sentAt = message.SentAt
            });
        }
    }
}