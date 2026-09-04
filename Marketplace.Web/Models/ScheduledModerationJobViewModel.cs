namespace Marketplace.Web.Models
{
    public class ScheduledModerationJobViewModel
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime? NextRunAtUtc { get; set; }
    }
}
