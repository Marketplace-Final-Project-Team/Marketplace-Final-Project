using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class WithdrawalRequest : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Business Business { get; set; } = null!;

        public decimal Amount { get; set; }

        public string CardNumber { get; set; } = null!;
        public string RecipientName { get; set; } = null!;

        public WithdrawalRequestStatusEnum Status { get; set; } = WithdrawalRequestStatusEnum.Pending;
        public string? Message { get; set; }
    }
}