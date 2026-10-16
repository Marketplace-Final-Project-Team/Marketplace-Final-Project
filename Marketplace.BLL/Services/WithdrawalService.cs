using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Marketplace.BLL.Services
{
    public class WithdrawalService : IWithdrawalService
    {
        private readonly AppDbContext _context;
        private readonly IBusinessRepository _businessRepo;
        private readonly IGenericRepository<WithdrawalRequest> _withdrawalRequestRepo;
        private readonly IPaymentService _paymentService;
        private readonly IMarketplaceBalanceService _marketplaceBalanceService;
        private readonly UserManager<User> _userManager;

        public WithdrawalService(
            AppDbContext context,
            IBusinessRepository businessRepo,
            IGenericRepository<WithdrawalRequest> withdrawalRequestRepo,
            IPaymentService paymentService,
            IMarketplaceBalanceService marketplaceBalanceService,
            UserManager<User> userManager
            )
        {
            _context = context;
            _businessRepo = businessRepo;
            _withdrawalRequestRepo = withdrawalRequestRepo;
            _paymentService = paymentService;
            _marketplaceBalanceService = marketplaceBalanceService;
            _userManager = userManager;
        }

        private static string MaskCard(string cardNumber) => cardNumber.Length >= 4
            ? $"•••• {cardNumber[^4..]}"
            : cardNumber;

        public async Task<ServiceResult> Create(Guid businessId, decimal amount, Guid? savedCardId, string? cardNumber, string? recipientName, bool rememberCard)
        {
            var business = await _context.Businesses
                .Include(b => b.SavedPayoutCards)
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Business not found." };
            }

            string effectiveCardNumber;
            string effectiveRecipientName;

            if (savedCardId.HasValue)
            {
            
                var chosenCard = business.SavedPayoutCards.FirstOrDefault(c => c.Id == savedCardId.Value);
                if (chosenCard is null)
                {
                    return new ServiceResult { Succeeded = false, Message = "Обрану картку не знайдено." };
                }

                effectiveCardNumber = chosenCard.CardNumber;
                effectiveRecipientName = chosenCard.RecipientName;
            }
            else if (!string.IsNullOrWhiteSpace(cardNumber) && !string.IsNullOrWhiteSpace(recipientName))
            {
 
                effectiveCardNumber = cardNumber.Trim();
                effectiveRecipientName = recipientName.Trim();

                if (rememberCard)
                {
                    var isFirstCard = business.SavedPayoutCards.Count == 0;
                    await _context.SavedPayoutCards.AddAsync(new SavedPayoutCard
                    {
                        BusinessId = businessId,
                        CardNumber = effectiveCardNumber,
                        RecipientName = effectiveRecipientName,
                        IsDefault = isFirstCard
                    });
                }
            }
            else
            {
       
                var defaultCard = business.SavedPayoutCards.FirstOrDefault(c => c.IsDefault)
                    ?? business.SavedPayoutCards.FirstOrDefault();

                if (defaultCard is null)
                {
                    return new ServiceResult { Succeeded = false, Message = "Вкажіть номер картки та отримувача." };
                }

                effectiveCardNumber = defaultCard.CardNumber;
                effectiveRecipientName = defaultCard.RecipientName;
            }

            var pendingTotal = await _context.WithdrawalRequests
                .Where(w => w.BusinessId == businessId && w.Status == WithdrawalStatus.Pending)
                .SumAsync(w => w.Amount);

            var availableBalance = business.Balance - business.HeldBalance;

            if (pendingTotal + amount > availableBalance)
            {
                return new ServiceResult { Succeeded = false, Message = "Insufficient balance for this withdrawal request." };
            }

            var withdrawalRequest = new WithdrawalRequest
            {
                BusinessId = businessId,
                Amount = amount,
                Status = WithdrawalStatus.Pending,
                CardNumber = effectiveCardNumber,
                RecipientName = effectiveRecipientName,
            };

            await _context.WithdrawalRequests.AddAsync(withdrawalRequest);
            await _context.SaveChangesAsync();


            var marketplaceBalance = await _marketplaceBalanceService.GetOrCreateTrackedAsync();

            if (marketplaceBalance.Balance >= amount)
            {
                var payoutSucceeded = await _paymentService.CreatePayoutAsync(
                    withdrawalRequest.Id, amount, effectiveCardNumber, effectiveRecipientName,
                    $"Автоматична виплата #{withdrawalRequest.Id}");

                if (payoutSucceeded)
                {
                    await _context.Businesses.Where(b => b.Id == businessId)
                        .ExecuteUpdateAsync(s => s.SetProperty(b => b.Balance, b => b.Balance - amount));

                    withdrawalRequest.Status = WithdrawalStatus.Completed;
                    withdrawalRequest.Message = "Виплачено автоматично.";
                    withdrawalRequest.ProcessedAt = DateTime.UtcNow;

                    _withdrawalRequestRepo.Update(withdrawalRequest);
                    await _context.SaveChangesAsync();

                    return new ServiceResult
                    {
                        Succeeded = true,
                        Message = "Заявку автоматично оброблено та виплачено.",
                    };
                }

            }

            return new ServiceResult
            {
                Succeeded = true,
                Message = "Withdrawal request created successfully.",
            };
        }

        public async Task<List<SavedPayoutCardDto>> GetSavedCardsAsync(Guid businessId)
        {
            return await _context.SavedPayoutCards
                .Where(c => c.BusinessId == businessId)
                .OrderByDescending(c => c.IsDefault)
                .ThenByDescending(c => c.CreatedAt)
                .Select(c => new SavedPayoutCardDto
                {
                    Id = c.Id,
                    MaskedCardNumber = c.CardNumber.Length >= 4 ? $"•••• {c.CardNumber.Substring(c.CardNumber.Length - 4)}" : c.CardNumber,
                    RecipientName = c.RecipientName,
                    IsDefault = c.IsDefault
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> DeleteSavedCardAsync(Guid businessId, Guid cardId)
        {
            var card = await _context.SavedPayoutCards
                .FirstOrDefaultAsync(c => c.Id == cardId && c.BusinessId == businessId);

            if (card is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Картку не знайдено." };
            }

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId);
            if (business?.ScheduledPayoutCardId == cardId)
            {
                return new ServiceResult { Succeeded = false, Message = "Ця картка використовується для запланованої автовиплати - спочатку оберіть іншу картку там або вимкніть автовиплату." };
            }

            var wasDefault = card.IsDefault;

            _context.SavedPayoutCards.Remove(card);
            await _context.SaveChangesAsync();


            if (wasDefault)
            {
                var nextCard = await _context.SavedPayoutCards
                    .Where(c => c.BusinessId == businessId)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (nextCard is not null)
                {
                    nextCard.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            return new ServiceResult { Succeeded = true, Message = "Картку видалено." };
        }

        public async Task<ServiceResult> SetScheduledPayoutSettingsAsync(Guid businessId, bool enabled, int? dayOfMonth, decimal? amount, Guid? savedCardId)
        {
            var business = await _context.Businesses
                .Include(b => b.SavedPayoutCards)
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Бізнес не знайдено." };
            }

            if (enabled)
            {
                if (dayOfMonth is null || dayOfMonth < 1 || dayOfMonth > 31)
                {
                    return new ServiceResult { Succeeded = false, Message = "Оберіть день місяця від 1 до 31." };
                }

                if (amount is null || amount <= 0)
                {
                    return new ServiceResult { Succeeded = false, Message = "Вкажіть суму, більшу за нуль." };
                }

                if (business.SavedPayoutCards.Count == 0)
                {
                    return new ServiceResult { Succeeded = false, Message = "Спочатку збережіть хоча б одну картку для виплат." };
                }

                var cardToUse = savedCardId.HasValue
                    ? business.SavedPayoutCards.FirstOrDefault(c => c.Id == savedCardId.Value)
                    : business.SavedPayoutCards.FirstOrDefault(c => c.IsDefault) ?? business.SavedPayoutCards.First();

                if (cardToUse is null)
                {
                    return new ServiceResult { Succeeded = false, Message = "Обрану картку не знайдено." };
                }

                business.ScheduledPayoutCardId = cardToUse.Id;
            }
            else
            {
                business.ScheduledPayoutCardId = null;
            }

            business.ScheduledPayoutEnabled = enabled;
            business.ScheduledPayoutDayOfMonth = enabled ? dayOfMonth : null;
            business.ScheduledPayoutAmount = enabled ? amount : null;

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Succeeded = true,
                Message = enabled ? "Заплановану автовиплату увімкнено." : "Заплановану автовиплату вимкнено."
            };
        }

        public async Task<IReadOnlyList<WithdrawalRequestDto>> GetAllWithdrawalsAsync(WithdrawalStatus? status)
        {
            var query = _context.WithdrawalRequests.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(w => w.Status == status.Value);
            }

            return await query.OrderByDescending(w => w.CreatedAt)
                .Select(w => new WithdrawalRequestDto
                {
                    Id = w.Id,
                    BusinessId = w.BusinessId,
                    BusinessName = w.Business.StoreName,
                    Amount = w.Amount,
                    CardNumber = w.CardNumber,
                    RecipientName = w.RecipientName,
                    Status = w.Status.ToString(),
                    CreatedAt = w.CreatedAt,
                    ProcessedAt = w.ProcessedAt,
                    ProcessedByUserId = w.ProcessedByUserId,
                    Message = w.Message
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<WithdrawalRequestDto>> GetForBusinessAsync(Guid businessId)
        {
            return await _context.WithdrawalRequests
                .Where(w => w.BusinessId == businessId)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new WithdrawalRequestDto
                {
                    Id = w.Id,
                    BusinessId = w.BusinessId,
                    BusinessName = w.Business.StoreName,
                    Amount = w.Amount,
                    CardNumber = w.CardNumber,
                    RecipientName = w.RecipientName,
                    Status = w.Status.ToString(),
                    CreatedAt = w.CreatedAt,
                    ProcessedAt = w.ProcessedAt,
                    ProcessedByUserId = w.ProcessedByUserId,
                    Message = w.Message
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> ApproveWithdrawalRequestAsync(Guid requestId, string adminUserId)
        {
            var request = await _withdrawalRequestRepo.GetByIdAsync(requestId);

            if (request == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Запит не знайдено." };
            }

            if (request.Status != WithdrawalStatus.Pending)
            {
                return new ServiceResult { Succeeded = false, Message = "Запит вже оброблено." };
            }

            var business = await _businessRepo.GetByIdAsync(request.BusinessId);

            if (business == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Бізнес не знайдено." };
            }

            if (business.Balance - business.HeldBalance < request.Amount)
            {
                request.Status = WithdrawalStatus.Failed;
                request.Message = "Недостатньо коштів на балансі бізнесу на момент обробки.";

                _withdrawalRequestRepo.Update(request);
                await _context.SaveChangesAsync();

                return new ServiceResult { Succeeded = false, Message = request.Message };
            }

            request.Status = WithdrawalStatus.Processing;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;

            _withdrawalRequestRepo.Update(request);
            await _context.SaveChangesAsync();

            var res = await _paymentService.CreatePayoutAsync(requestId, request.Amount, request.CardNumber, request.RecipientName, $"Оплата запиту #{requestId}");

            if (!res)
            {
                var message = $"Не вдалося здійснити виплату для запиту.";

                request.Status = WithdrawalStatus.Failed;
                request.Message = message;

                _withdrawalRequestRepo.Update(request);
                await _context.SaveChangesAsync();

                return new ServiceResult
                {
                    Succeeded = false,
                    Message = message
                };
            }

            await _context.Businesses.Where(b => b.Id == business.Id).ExecuteUpdateAsync(s => s.SetProperty(b => b.Balance, b => b.Balance - request.Amount));
            request.Status = WithdrawalStatus.Completed;
            _withdrawalRequestRepo.Update(request);

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Succeeded = true,
                Message = string.Empty
            };
        }

        public async Task<ServiceResult> RejectWithdrawalRequestAsync(Guid requestId, string reason, string adminUserId)
        {
            var request = await _withdrawalRequestRepo.GetByIdAsync(requestId);

            if (request == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Запит не знайдено." };
            }

            if (request.Status != WithdrawalStatus.Pending)
            {
                return new ServiceResult { Succeeded = false, Message = "Запит вже оброблено." };
            }

            request.Status = WithdrawalStatus.Rejected;
            request.ProcessedByUserId = adminUserId;
            request.ProcessedAt = DateTime.UtcNow;
            request.Message = reason;

            _withdrawalRequestRepo.Update(request);
            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Succeeded = true,
                Message = string.Empty
            };
        }
    }
}