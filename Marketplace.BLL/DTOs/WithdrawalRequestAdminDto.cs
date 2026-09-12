using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class WithdrawalRequestAdminDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string CardNumber { get; set; } = null!;
        public string RecipientName { get; set; } = null!;
        public WithdrawalStatus Status { get; set; }
        public string? RejectionReason { get; set; }
    }
}
