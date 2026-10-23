namespace Marketplace.BLL.DTOs
{
    public class TicketChatPageDto
    {
        public AccessStatus Status { get; init; }
        public Guid TicketId { get; init; }
        public string Category { get; init; } = "";
        public IReadOnlyList<SupportTicketMessageDto> Messages { get; init; } = [];
    }

    public class TicketStatusDto
    {
        public AccessStatus Access { get; init; }
        public string? TicketStatus { get; init; }
    }
}
