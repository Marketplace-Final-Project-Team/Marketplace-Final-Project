using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class SupportTicketMessage : BaseEntity
    {
        public Guid SupportTicketId { get; set; }
        public SupportTicket SupportTicket { get; set; } = null!;
        public string SenderId { get; set; } = string.Empty;
        public bool IsFromAdmin { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}