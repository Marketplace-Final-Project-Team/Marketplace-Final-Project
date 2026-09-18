namespace Marketplace.Web.Models
{
    public class JobsPageViewModel
    {
        public bool IsSchedulerRunning { get; set; }
        public List<ScheduledModerationJobViewModel> ScheduledJobs { get; set; } = new();
        public List<BusinessActivityLogItemViewModel> RecentDecisions { get; set; } = new();
        public List<Marketplace.BLL.DTOs.PostSaleRequestDto> OpenReturnRequests { get; set; } = new();
        public List<Marketplace.BLL.DTOs.SupportTicketDto> OpenTickets { get; set; } = new();
    }
}