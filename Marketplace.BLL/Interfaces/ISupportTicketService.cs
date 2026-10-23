using Marketplace.BLL.DTOs;
using Marketplace.BLL.Common;

namespace Marketplace.BLL.Interfaces
{
    public interface ISupportTicketService
    {
        Task<ServiceResult> CreateTicketAsync(string userId, string category, string message);
        Task<SupportTicketDto?> GetActiveTicketForUserAsync(string userId);
        Task<List<SupportTicketDto>> GetOpenTicketsForAdminAsync();
        Task<ServiceResult> AdminOpenChatAsync(Guid ticketId);

        Task<ServiceResult> CloseTicketAsync(Guid ticketId);

        Task<SupportTicketMessageDto?> SaveMessageAsync(Guid ticketId, string senderId, bool isFromAdmin, string text);
        Task<List<SupportTicketMessageDto>> GetMessagesAsync(Guid ticketId);
        Task MarkAsReadAsync(Guid ticketId, bool byAdmin);
        Task<int> GetUnreadCountAsync(Guid ticketId, bool byAdmin);
        Task<TicketStatusDto> GetTicketStatusAsync(Guid ticketId, string? userId);
        Task<TicketChatPageDto> GetTicketChatAsync(Guid ticketId, string? userId, bool isAdmin);
    }
}