using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class MarketplaceBalanceService : IMarketplaceBalanceService
    {
        private const int DefaultCommissionPercent = 10;

        private readonly AppDbContext _context;
        private readonly ISystemSettingsService _settingsService;

        public MarketplaceBalanceService(AppDbContext context, ISystemSettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<decimal> GetBalanceAsync()
        {
            var balance = await _context.MarketplaceBalances.AsNoTracking().FirstOrDefaultAsync();
            return balance?.Balance ?? 0m;
        }

        // Returns the tracked singleton row so a caller already inside a unit of work (e.g.
        // OrderService.ChangeOrderStatus) can mutate .Balance directly and have it persist
        // together with the rest of that method's SaveChangesAsync — same pattern as
        // Business.Balance elsewhere in this codebase.
        public async Task<MarketplaceBalance> GetOrCreateTrackedAsync()
        {
            var balance = await _context.MarketplaceBalances.FirstOrDefaultAsync();
            if (balance is null)
            {
                balance = new MarketplaceBalance { Balance = 0 };
                _context.MarketplaceBalances.Add(balance);
                await _context.SaveChangesAsync();
            }

            return balance;
        }

        public Task<int> GetCommissionPercentAsync()
        {
            return _settingsService.GetIntAsync("Marketplace", "CommissionPercent", DefaultCommissionPercent);
        }

        public Task SetCommissionPercentAsync(int percent, string? actorUserId)
        {
            var clamped = Math.Clamp(percent, 0, 100);
            return _settingsService.SetAsync("Marketplace", "CommissionPercent", clamped.ToString(), false, actorUserId);
        }
    }
}
