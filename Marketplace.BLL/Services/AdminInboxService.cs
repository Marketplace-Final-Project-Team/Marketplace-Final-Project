using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Jobs;

namespace Marketplace.BLL.Services
{
    public class AdminInboxService : IAdminInboxService
    {
        private readonly IPostSaleService _postSaleService;
        private readonly IRefundChatService _refundChatService;
        private readonly ISupportTicketService _supportTicketService;
        private readonly IAdminService _adminService;
        private readonly IActivityLogService _activityLog;

        public AdminInboxService(
            IPostSaleService postSaleService,
            IRefundChatService refundChatService,
            ISupportTicketService supportTicketService,
            IAdminService adminService,
            IActivityLogService activityLog)
        {
            _postSaleService = postSaleService;
            _refundChatService = refundChatService;
            _supportTicketService = supportTicketService;
            _adminService = adminService;
            _activityLog = activityLog;
        }

        public async Task<AdminInboxDto> GetInboxAsync()
        {
            var openReturnRequests = await _postSaleService.GetAllOpenRequestsAsync();
            foreach (var request in openReturnRequests)
            {
                request.UnreadFromBuyer = await _refundChatService.GetUnreadCountAsync(request.Id, isSellerChannel: false, byAdmin: true);
                request.UnreadFromSeller = await _refundChatService.GetUnreadCountAsync(request.Id, isSellerChannel: true, byAdmin: true);
            }

            return new AdminInboxDto
            {
                OpenReturnRequests = openReturnRequests,
                OpenTickets = await _supportTicketService.GetOpenTicketsForAdminAsync(),
                AdultRequests = await _adminService.GetPendingAdultRequestsAsync()
            };
        }

        public async Task<IReadOnlyList<BusinessActivityLogDto>> GetRecentModerationDecisionsAsync(int take = 30)
        {
            return (await _activityLog.GetRecentBusinessActionsAsync())
                .Where(l => l.Action == ProductModerationJob.ActivityLogAction)
                .Take(take)
                .Select(
                l => new BusinessActivityLogDto 
                {
                    BusinessName = l.BusinessName,
                    ActorUserId = l.ActorUserId,
                    Action = l.Action,
                    Description = l.Description,
                    CreatedAt = l.CreatedAt
                })
                .ToList();
        }

        public async Task<AdminInboxCountsDto> GetCountsAsync()
        {
            return new AdminInboxCountsDto
            {
                ReturnCount = (await _postSaleService.GetAllOpenRequestsAsync()).Count,
                TicketCount = (await _supportTicketService.GetOpenTicketsForAdminAsync()).Count,
                AdultCount = (await _adminService.GetPendingAdultRequestsAsync()).Count
            };
        }
    }
}