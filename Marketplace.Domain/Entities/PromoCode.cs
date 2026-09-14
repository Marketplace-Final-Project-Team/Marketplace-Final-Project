using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class PromoCode : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Business Business { get; set; } = null!;

        public string Code { get; set; } = string.Empty;
        public int Percent { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsSingleUse { get; set; }
        public bool IsUsed { get; set; }

        public bool IsActive => StartDate <= DateTime.UtcNow && EndDate >= DateTime.UtcNow && !(IsSingleUse && IsUsed);
    }
}
