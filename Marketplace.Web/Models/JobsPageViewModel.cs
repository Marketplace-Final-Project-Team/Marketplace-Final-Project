namespace Marketplace.Web.Models
{
    public class JobsPageViewModel
    {
        public bool IsSchedulerRunning { get; set; }
        public List<ScheduledModerationJobViewModel> ScheduledJobs { get; set; } = new();
        public List<RecurringJobViewModel> RecurringJobs { get; set; } = new();
        public List<BusinessActivityLogItemViewModel> RecentDecisions { get; set; } = new();
        public bool HasCustomApiKey { get; set; }
        public string? ModerationModel { get; set; }
    }
}
