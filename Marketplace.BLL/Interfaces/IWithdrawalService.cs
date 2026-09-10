using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.Interfaces
{
    public interface IWithdrawalService
    {
        Task<ServiceResult> Create(Guid businessId, decimal amount, string cardNumber, string recipientName);
        Task<IReadOnlyList<WithdrawalRequestDto>> GetAllWithdrawalsAsync(WithdrawalStatus? status);
    }
}
