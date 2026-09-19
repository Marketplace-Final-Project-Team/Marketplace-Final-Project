using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class RefundChatMessage : BaseEntity
    {
        public Guid PostSaleRequestId { get; set; }
        public PostSaleRequest PostSaleRequest { get; set; } = null!;

        public string SenderId { get; set; } = string.Empty;
        public bool IsFromAdmin { get; set; }

        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public bool IsSellerChannel { get; set; }
    }
}