namespace Marketplace.Web.Models
{
    public class RecurringJobViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? NextRunAtUtc { get; set; }
        public DateTime? PreviousRunAtUtc { get; set; }
    }
}
