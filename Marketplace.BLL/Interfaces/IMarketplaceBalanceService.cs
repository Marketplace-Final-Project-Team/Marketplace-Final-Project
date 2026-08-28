using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IMarketplaceBalanceService
    {
        Task<decimal> GetBalanceAsync();
        Task<MarketplaceBalance> GetOrCreateTrackedAsync();
        Task<int> GetCommissionPercentAsync();
        Task SetCommissionPercentAsync(int percent, string? actorUserId);
    }
}
