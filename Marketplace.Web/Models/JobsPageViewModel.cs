namespace Marketplace.Web.Models
{
    public class JobsPageViewModel
    {
        public bool IsSchedulerRunning { get; set; }
        public List<ScheduledModerationJobViewModel> ScheduledJobs { get; set; } = new();
        public List<RecurringJobViewModel> RecurringJobs { get; set; } = new();
        public List<BusinessActivityLogItemViewModel> RecentDecisions { get; set; } = new();
        public List<Marketplace.BLL.DTOs.PostSaleRequestDto> OpenReturnRequests { get; set; } = new();
        public List<Marketplace.BLL.DTOs.SupportTicketDto> OpenTickets { get; set; } = new();
        public List<AdultRequestViewModel> AdultRequests { get; set; } = new();
        public bool HasCustomApiKey { get; set; }
        public string? ModerationModel { get; set; }
    }

    public class AdultRequestViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }
}