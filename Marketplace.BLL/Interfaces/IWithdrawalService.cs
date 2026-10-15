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
     
        Task<ServiceResult> Create(Guid businessId, decimal amount, Guid? savedCardId, string? cardNumber, string? recipientName, bool rememberCard);

        Task<List<SavedPayoutCardDto>> GetSavedCardsAsync(Guid businessId);
        Task<ServiceResult> DeleteSavedCardAsync(Guid businessId, Guid cardId);

        Task<ServiceResult> SetScheduledPayoutSettingsAsync(Guid businessId, bool enabled, int? dayOfMonth, decimal? amount, Guid? savedCardId);

        Task<IReadOnlyList<WithdrawalRequestDto>> GetAllWithdrawalsAsync(WithdrawalStatus? status);
        Task<IReadOnlyList<WithdrawalRequestDto>> GetForBusinessAsync(Guid businessId);
        Task<ServiceResult> ApproveWithdrawalRequestAsync(Guid requestId, string adminUserId);
        Task<ServiceResult> RejectWithdrawalRequestAsync(Guid requestId, string reason, string adminUserId);
    }
}