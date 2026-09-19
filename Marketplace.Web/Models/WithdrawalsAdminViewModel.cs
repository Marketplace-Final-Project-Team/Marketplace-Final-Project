using Marketplace.BLL.DTOs;
using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class WithdrawalsAdminViewModel
    {
        public WithdrawalStatus? StatusFilter { get; set; }
        public IReadOnlyList<WithdrawalRequestDto> Withdrawals { get; set; } = new List<WithdrawalRequestDto>();
    }
}
