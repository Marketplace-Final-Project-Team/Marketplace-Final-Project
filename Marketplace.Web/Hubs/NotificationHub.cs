using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Marketplace.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}