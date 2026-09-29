using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetUnreadCountForBuyerAsync(string userId)
        {
            var statusChangedCount = await _context.PostSaleRequests
                .CountAsync(r => r.BuyerId == userId
                    && r.Status != PostSaleRequestStatus.Pending
                    && r.StatusChangedAt != null
                    && (r.BuyerLastViewedAt == null || r.BuyerLastViewedAt < r.StatusChangedAt));

            var refundChatUnread = await _context.RefundChatMessages
                .CountAsync(m => m.PostSaleRequest.BuyerId == userId
                    && !m.IsSellerChannel
                    && m.IsFromAdmin
                    && !m.IsRead);

            var supportUnread = await _context.SupportTicketMessages
                .CountAsync(m => m.SupportTicket.UserId == userId
                    && m.IsFromAdmin
                    && !m.IsRead);

            return statusChangedCount + refundChatUnread + supportUnread;
        }

        public async Task<List<NotificationItemDto>> GetRecentNotificationsAsync(string userId, int take = 15)
        {
            var items = new List<NotificationItemDto>();

            var changedRequests = await _context.PostSaleRequests
                .Where(r => r.BuyerId == userId
                    && r.Status != PostSaleRequestStatus.Pending
                    && r.StatusChangedAt != null
                    && (r.BuyerLastViewedAt == null || r.BuyerLastViewedAt < r.StatusChangedAt))
                .Include(r => r.OrderItem)
                .OrderByDescending(r => r.StatusChangedAt)
                .Take(take)
                .ToListAsync();

            foreach (var r in changedRequests)
            {
                var statusText = r.Status switch
                {
                    PostSaleRequestStatus.AwaitingSellerConfirmation => "продавець розглядає ваше повернення",
                    PostSaleRequestStatus.Refunded => "кошти повернуто",
                    PostSaleRequestStatus.Rejected => "у поверненні відмовлено",
                    PostSaleRequestStatus.SellerDisputed => "продавець відхилив, очікує рішення адміна",
                    _ => "статус змінено"
                };

                items.Add(new NotificationItemDto
                {
                    Type = "ReturnStatus",
                    Text = $"Заявка на повернення «{r.OrderItem?.ProductName}»: {statusText}",
                    Time = r.StatusChangedAt ?? r.CreatedAt,
                    LinkController = "Order",
                    LinkAction = "Details",
                    LinkId = r.OrderId
                });
            }

            var refundMessages = await _context.RefundChatMessages
                .Where(m => m.PostSaleRequest.BuyerId == userId && !m.IsSellerChannel && m.IsFromAdmin && !m.IsRead)
                .Include(m => m.PostSaleRequest)
                    .ThenInclude(r => r.OrderItem)
                .OrderByDescending(m => m.SentAt)
                .Take(take)
                .ToListAsync();

            foreach (var m in refundMessages)
            {
                items.Add(new NotificationItemDto
                {
                    Type = "RefundChat",
                    Text = $"Нове повідомлення від адміністратора щодо «{m.PostSaleRequest.OrderItem?.ProductName}»",
                    Time = m.SentAt,
                    LinkController = "Support",
                    LinkAction = "RefundChat",
                    LinkId = m.PostSaleRequestId
                });
            }

            var supportMessages = await _context.SupportTicketMessages
                .Where(m => m.SupportTicket.UserId == userId && m.IsFromAdmin && !m.IsRead)
                .Include(m => m.SupportTicket)
                .OrderByDescending(m => m.SentAt)
                .Take(take)
                .ToListAsync();

            foreach (var m in supportMessages)
            {
                items.Add(new NotificationItemDto
                {
                    Type = "SupportTicket",
                    Text = $"Відповідь адміністратора у зверненні «{m.SupportTicket.Category}»",
                    Time = m.SentAt,
                    LinkController = "Support",
                    LinkAction = "TicketChat",
                    LinkId = m.SupportTicketId
                });
            }

            return items.OrderByDescending(i => i.Time).Take(take).ToList();
        }

        public async Task MarkOrderViewedAsync(string userId, Guid orderId)
        {
            var requests = await _context.PostSaleRequests
                .Where(r => r.BuyerId == userId && r.OrderId == orderId)
                .ToListAsync();

            if (requests.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var r in requests)
            {
                r.BuyerLastViewedAt = now;
            }

            await _context.SaveChangesAsync();
        }
    }
}