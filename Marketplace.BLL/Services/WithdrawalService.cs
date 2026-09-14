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
        private readonly UserManager<User> _userManager;

        public WithdrawalService(
            AppDbContext context, 
            IBusinessRepository businessRepo, 
            IGenericRepository<WithdrawalRequest> withdrawalRequestRepo, 
            IPaymentService paymentService,
            UserManager<User> userManager
            )
        {
            _context = context;
            _businessRepo = businessRepo;
            _withdrawalRequestRepo = withdrawalRequestRepo;
            _paymentService = paymentService;
            _userManager = userManager;
        }

        public async Task<ServiceResult> Create(Guid businessId,decimal amount, string cardNumber, string recipientName)
        {
            var business = await _businessRepo.GetByIdAsync(businessId);
            
            if (business == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Business not found." };
            }

            var pendingTotal = await _context.WithdrawalRequests
                .Where(w => w.BusinessId == businessId && w.Status == WithdrawalStatus.Pending)
                .SumAsync(w => w.Amount);

            if (pendingTotal + amount > business.Balance)
            {
                return new ServiceResult { Succeeded = false, Message = "Insufficient balance for this withdrawal request." };
            }

            var withdrawalRequest = new WithdrawalRequest
            {
                BusinessId = businessId,
                Amount = amount,
                
                Status = WithdrawalStatus.Pending,
                CardNumber = cardNumber,
                RecipientName = recipientName,
                

            };

            await _context.WithdrawalRequests.AddAsync(withdrawalRequest);
            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Succeeded = true,
                Message = "Withdrawal request created successfully.",
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

            if (business.Balance < request.Amount)
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
