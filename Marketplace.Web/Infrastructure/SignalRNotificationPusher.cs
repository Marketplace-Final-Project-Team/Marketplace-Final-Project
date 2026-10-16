using Marketplace.BLL.Interfaces;
using Marketplace.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Marketplace.Web.Infrastructure
{
    public class SignalRNotificationPusher : INotificationPusher
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PushToUserAsync(string userId, string text, string type)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                text,
                type
            });
        }
    }
}