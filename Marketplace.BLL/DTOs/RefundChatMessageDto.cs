using System;

namespace Marketplace.BLL.DTOs
{
    public class RefundChatMessageDto
    {
        public Guid Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public bool IsFromAdmin { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class RefundChatThreadDto
    {
        public Guid PostSaleRequestId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string LastMessageText { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}