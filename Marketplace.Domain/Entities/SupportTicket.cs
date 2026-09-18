using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class SupportTicket : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Pending;
        public DateTime? ClosedAt { get; set; }
    }
}