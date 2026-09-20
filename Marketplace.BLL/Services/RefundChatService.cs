using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketplace.BLL.Services
{
    public class RefundChatService : IRefundChatService
    {
        private readonly AppDbContext _context;

        public RefundChatService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RefundChatMessageDto?> SaveMessageAsync(Guid postSaleRequestId, string senderId, bool isFromAdmin, bool isSellerChannel, string text)
        {
            var requestExists = await _context.PostSaleRequests.AnyAsync(r => r.Id == postSaleRequestId);
            if (!requestExists)
            {
                return null;
            }

            var message = new RefundChatMessage
            {
                PostSaleRequestId = postSaleRequestId,
                SenderId = senderId,
                IsFromAdmin = isFromAdmin,
                IsSellerChannel = isSellerChannel,
                Text = text,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.RefundChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return new RefundChatMessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                IsFromAdmin = message.IsFromAdmin,
                Text = message.Text,
                SentAt = message.SentAt
            };
        }

        public async Task<List<RefundChatMessageDto>> GetMessagesAsync(Guid postSaleRequestId, bool isSellerChannel)
        {
            return await _context.RefundChatMessages
                .Where(m => m.PostSaleRequestId == postSaleRequestId && m.IsSellerChannel == isSellerChannel)
                .OrderBy(m => m.SentAt)
                .Select(m => new RefundChatMessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    IsFromAdmin = m.IsFromAdmin,
                    Text = m.Text,
                    SentAt = m.SentAt
                })
                .ToListAsync();
        }

        public async Task<List<RefundChatThreadDto>> GetOpenThreadsForAdminAsync()
        {
            var activeRequests = await _context.PostSaleRequests
                .Include(r => r.Order)
                .ThenInclude(o => o.User)
                .Where(r => r.Status != PostSaleRequestStatus.Approved &&
                            r.Status != PostSaleRequestStatus.Rejected &&
                            r.Status != PostSaleRequestStatus.Refunded)
                .ToListAsync();

            var threads = new List<RefundChatThreadDto>();

            foreach (var req in activeRequests)
            {

                var lastMessage = await _context.RefundChatMessages
                    .Where(m => m.PostSaleRequestId == req.Id && !m.IsSellerChannel)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.RefundChatMessages
                    .CountAsync(m => m.PostSaleRequestId == req.Id && !m.IsSellerChannel && !m.IsFromAdmin && !m.IsRead);

                threads.Add(new RefundChatThreadDto
                {
                    PostSaleRequestId = req.Id,
                    BuyerName = req.Order?.User?.UserName ?? req.Order?.User?.Email ?? "Невідомий покупець",
                    OrderNumber = req.OrderId.ToString()[..8].ToUpperInvariant(),
                    Reason = req.Reason.ToString(),
                    LastMessageText = lastMessage?.Text ?? "Чат розпочато",
                    LastMessageAt = lastMessage?.SentAt ?? req.CreatedAt,
                    UnreadCount = unreadCount
                });
            }

            return threads.OrderByDescending(t => t.LastMessageAt).ToList();
        }

        public async Task MarkAsReadAsync(Guid postSaleRequestId, bool byAdmin, bool isSellerChannel)
        {
            var unreadMessages = await _context.RefundChatMessages
                .Where(m => m.PostSaleRequestId == postSaleRequestId
                    && m.IsSellerChannel == isSellerChannel
                    && m.IsRead == false
                    && m.IsFromAdmin != byAdmin)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                _context.RefundChatMessages.UpdateRange(unreadMessages);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid postSaleRequestId, bool isSellerChannel, bool byAdmin)
        {
            return await _context.RefundChatMessages
                .CountAsync(m => m.PostSaleRequestId == postSaleRequestId
                    && m.IsSellerChannel == isSellerChannel
                    && m.IsFromAdmin != byAdmin
                    && !m.IsRead);
        }

    }
}