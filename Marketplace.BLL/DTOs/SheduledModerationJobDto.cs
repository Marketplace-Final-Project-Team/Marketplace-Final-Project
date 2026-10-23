namespace Marketplace.BLL.DTOs
{
    public class SheduledModerationJobDto
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string? EntityName { get; set; }
        public DateTime? NextRunAtUtc { get; set; }
    }
}
