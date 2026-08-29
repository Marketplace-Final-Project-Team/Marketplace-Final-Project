using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    // Singleton — exactly one row, tracking the platform's accumulated commission revenue.
    public class MarketplaceBalance : BaseEntity
    {
        public decimal Balance { get; set; } = 0;
    }
}
