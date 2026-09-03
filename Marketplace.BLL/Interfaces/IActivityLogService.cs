using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IActivityLogService
    {
        Task LogUserActionAsync(string userId, string action, string description, string? actorUserId = null, Guid? relatedEntityId = null);
        Task LogBusinessActionAsync(Guid businessId, string action, string description, string? actorUserId = null, Guid? relatedEntityId = null);
        Task<List<UserActionLog>> GetRecentUserActionsAsync(int take = 200);
        Task<List<BusinessActionLog>> GetRecentBusinessActionsAsync(int take = 200);
        Task<List<BusinessActionLog>> GetRecentBusinessActionsAsync(Guid businessId, int take = 15);
    }
}
