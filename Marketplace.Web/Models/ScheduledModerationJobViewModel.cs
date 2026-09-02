namespace Marketplace.Web.Models
{
    public class ScheduledModerationJobViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime? NextRunAtUtc { get; set; }
    }
}
