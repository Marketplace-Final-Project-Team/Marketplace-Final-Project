using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IShedulerAdminService
    {
        Task<bool> PauseAsync();
        Task<bool> ResumeAsync();
        Task<bool> TriggerModerationJobAsync(Guid entityId, string entityType);
        Task<bool> TriggerRecurringJobAsync(string groupName, string jobName);
        Task<IReadOnlyList<SheduledModerationJobDto>> GetSheduledJobsAsync();
        Task<IReadOnlyList<RecurringJobDto>> GetReccuringJobsAsync();
        Task<bool> IsRunningAsync();
    }
}
