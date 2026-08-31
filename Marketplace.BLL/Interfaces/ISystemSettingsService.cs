using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface ISystemSettingsService
    {
        Task<string?> GetAsync(string category, string key);
        Task<int> GetIntAsync(string category, string key, int fallback);
        Task<List<SystemSetting>> GetAllAsync();
        Task<List<SystemSetting>> GetByCategoryAsync(string category);
        Task SetAsync(string category, string key, string value, bool isSecret, string? actorUserId = null);
        Task SeedIfMissingAsync(string category, string key, string value, bool isSecret);
    }
}
