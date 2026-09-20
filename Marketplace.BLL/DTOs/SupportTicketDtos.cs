using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class SupportTicketDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public SupportTicketStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UnreadForAdmin { get; set; }
    }

    public class SupportTicketMessageDto
    {
        public Guid Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public bool IsFromAdmin { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}