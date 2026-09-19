using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Marketplace.BLL.Common;

namespace Marketplace.BLL.Services
{
    public class SupportTicketService : ISupportTicketService
    {
        private readonly AppDbContext _context;

        public SupportTicketService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult> CreateTicketAsync(string userId, string category, string message)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return new ServiceResult { Succeeded = false, Message = "Оберіть тему звернення" };
            }

            if (string.IsNullOrWhiteSpace(message) || message.Trim().Length < 10)
            {
                return new ServiceResult { Succeeded = false, Message = "Опишіть ситуацію детальніше — щонайменше 10 символів" };
            }

            var alreadyActive = await _context.SupportTickets
                .AnyAsync(t => t.UserId == userId && (t.Status == SupportTicketStatus.Pending || t.Status == SupportTicketStatus.Open));

            if (alreadyActive)
            {
                return new ServiceResult { Succeeded = false, Message = "У вас вже є активне звернення. Дочекайтесь відповіді або закриття поточного." };
            }

            var ticket = new SupportTicket
            {
                UserId = userId,
                Category = category.Trim(),
                Message = message.Trim(),
                Status = SupportTicketStatus.Pending
            };

            await _context.SupportTickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Звернення надіслано. Очікуйте на відповідь.", EntityId = ticket.Id };
        }

        public async Task<SupportTicketDto?> GetActiveTicketForUserAsync(string userId)
        {
            var ticket = await _context.SupportTickets
                .Include(t => t.User)
                .Where(t => t.UserId == userId && (t.Status == SupportTicketStatus.Pending || t.Status == SupportTicketStatus.Open))
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (ticket is null)
            {
                return null;
            }

            var unread = await GetUnreadCountAsync(ticket.Id, byAdmin: false);

            return ToDto(ticket, unread);
        }

        public async Task<List<SupportTicketDto>> GetOpenTicketsForAdminAsync()
        {
            var tickets = await _context.SupportTickets
                .Include(t => t.User)
                .Where(t => t.Status == SupportTicketStatus.Pending || t.Status == SupportTicketStatus.Open)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            var result = new List<SupportTicketDto>();
            foreach (var ticket in tickets)
            {
                var unread = await GetUnreadCountAsync(ticket.Id, byAdmin: true);
                result.Add(ToDto(ticket, unread));
            }

            return result;
        }

        public async Task<ServiceResult> AdminOpenChatAsync(Guid ticketId)
        {
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Звернення не знайдено" };
            }

            if (ticket.Status == SupportTicketStatus.Pending)
            {
                ticket.Status = SupportTicketStatus.Open;
                ticket.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new ServiceResult { Succeeded = true, Message = string.Empty, EntityId = ticket.Id };
        }

        public async Task<ServiceResult> CloseTicketAsync(Guid ticketId)
        {
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Звернення не знайдено" };
            }

            ticket.Status = SupportTicketStatus.Closed;
            ticket.ClosedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Звернення закрито." };
        }

        public async Task<SupportTicketMessageDto?> SaveMessageAsync(Guid ticketId, string senderId, bool isFromAdmin, string text)
        {
            var exists = await _context.SupportTickets.AnyAsync(t => t.Id == ticketId);
            if (!exists)
            {
                return null;
            }

            var message = new SupportTicketMessage
            {
                SupportTicketId = ticketId,
                SenderId = senderId,
                IsFromAdmin = isFromAdmin,
                Text = text,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _context.SupportTicketMessages.AddAsync(message);
            await _context.SaveChangesAsync();

            return new SupportTicketMessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                IsFromAdmin = message.IsFromAdmin,
                Text = message.Text,
                SentAt = message.SentAt
            };
        }

        public async Task<List<SupportTicketMessageDto>> GetMessagesAsync(Guid ticketId)
        {
            return await _context.SupportTicketMessages
                .Where(m => m.SupportTicketId == ticketId)
                .OrderBy(m => m.SentAt)
                .Select(m => new SupportTicketMessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    IsFromAdmin = m.IsFromAdmin,
                    Text = m.Text,
                    SentAt = m.SentAt
                })
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid ticketId, bool byAdmin)
        {
            var unread = await _context.SupportTicketMessages
                .Where(m => m.SupportTicketId == ticketId && m.IsFromAdmin != byAdmin && !m.IsRead)
                .ToListAsync();

            if (unread.Count > 0)
            {
                foreach (var m in unread)
                {
                    m.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid ticketId, bool byAdmin)
        {
            return await _context.SupportTicketMessages
                .CountAsync(m => m.SupportTicketId == ticketId && m.IsFromAdmin != byAdmin && !m.IsRead);
        }

        private static SupportTicketDto ToDto(SupportTicket t, int unread) => new()
        {
            Id = t.Id,
            UserId = t.UserId,
            UserEmail = t.User?.Email ?? string.Empty,
            Category = t.Category,
            Message = t.Message,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            UnreadForAdmin = unread
        };
    }
}