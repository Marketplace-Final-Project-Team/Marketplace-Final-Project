using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class SavedPayoutCard : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Business Business { get; set; } = null!;

        public string CardNumber { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }
}