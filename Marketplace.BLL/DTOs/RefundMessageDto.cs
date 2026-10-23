using Marketplace.BLL.DTOs;

public class RefundChatPageDto
{
    public AccessStatus Status { get; init; }
    public Guid RequestId { get; init; }
    public Guid OrderId { get; init; }
    public Guid? BusinessId { get; init; }
    public string? ProductName { get; init; }
    public List<RefundChatMessageDto> Messages { get; init; } = new();
}

public enum AccessStatus { Ok, NotFound, Forbidden }