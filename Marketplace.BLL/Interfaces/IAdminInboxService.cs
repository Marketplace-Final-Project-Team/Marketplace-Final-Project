using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IAdminInboxService
    {
        Task<AdminInboxDto> GetInboxAsync();
        Task<IReadOnlyList<BusinessActivityLogDto>> GetRecentModerationDecisionsAsync(int take = 30);  
        Task<AdminInboxCountsDto> GetCountsAsync();
    }
}
