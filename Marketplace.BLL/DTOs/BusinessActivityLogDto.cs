namespace Marketplace.BLL.DTOs
{
    public class BusinessActivityLogDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string? ActorUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
