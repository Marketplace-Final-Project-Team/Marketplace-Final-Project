using Marketplace.BLL.Common;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
namespace Marketplace.BLL.Services
{
    public class WithdrawalService : IWithdrawalService
    {
        private readonly AppDbContext _context;
        private readonly IBusinessRepository _businessRepo;
        public WithdrawalService(AppDbContext context, IBusinessRepository businessRepo)
        {
            _context = context;
            _businessRepo = businessRepo;
        }

        public async Task<ServiceResult> Create ( Guid businessId,decimal amount, string cardNumber, string recipientName )
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
        public async Task<IReadOnlyList<WithdrawalRequestDto>> GetAllWithdrawalsAsync (WithdrawalStatus? status)
        {
            var query = _context.WithdrawalRequests.AsQueryable();
            
            if (status.HasValue)
            {
                query = query.Where(w => w.Status == status.Value);
            }
            return await query
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
                    RejectionReason = w.RejectionReason
                })
                .ToListAsync();

        }
    }
}
