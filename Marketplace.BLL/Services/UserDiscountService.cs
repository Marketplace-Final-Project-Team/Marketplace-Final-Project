using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.Services
{
    public class UserDiscountService : IUserDiscountService
    {
        public readonly AppDbContext _context;
        public readonly UserManager<User> _usermanager;
        private readonly IBusinessRepository _businessRepo;

        private readonly ISystemSettingsService _settingsService;


        public UserDiscountService(AppDbContext appDbContext, UserManager<User> usermanager, IBusinessRepository businessRepo, ISystemSettingsService settingsService)
        {
            _context = appDbContext;
            _usermanager = usermanager;
            _businessRepo = businessRepo;
            _settingsService = settingsService;
        }
        public async Task<ServiceResult> SetUserDiscount(string userId, Guid businessId, decimal percent,DateTime startDate,DateTime endDate)
        {
            var user = await _usermanager.FindByIdAsync( userId);
            if (user == null) {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "User not found"
                };
                
            }
            var business = await _businessRepo.GetByIdAsync(businessId);
            if (business == null) {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Business not found"
                };
            }
            var active_discount = await _context.UserDiscounts
                .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.BusinessId == businessId && ud.EndDate > DateTime.UtcNow && ud.StartDate <= DateTime.UtcNow);
            if (active_discount != null) {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Active discount already exists for this user and business"
                };
            }
            var maxDiscountPercent = await _settingsService.GetIntAsync("BusinessRules", "MaxDiscountPercent", 90);
            if (percent < 1 || percent > maxDiscountPercent)
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = $"Знижка повинна бути в межах від 1 до {maxDiscountPercent} зараз знижка { percent}"
                };
            
            if (startDate >= endDate)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Дата початку знижки повинна бути менше дати закінчення"
                };
            }
            if (startDate < DateTime.UtcNow.Date)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Дата початку знижки не може бути в минулому"
                };
            }
            if (endDate < DateTime.UtcNow)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Дата закінчення знижки не може бути в минулому"
                };
            }


            var discount = new UserDiscount
            {
                UserId = userId,
                BusinessId = businessId,
                Percent = percent,
                StartDate = startDate,
                EndDate = endDate
            };


            await _context.UserDiscounts.AddAsync(discount);
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Знижка успішно встановлена"

            };


        }

        public async Task<ServiceResult> RemoveUserDiscount (string userId,Guid businessId)
        {
            var user = await _usermanager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "User not found"
                };

            }
            var business = await _businessRepo.GetByIdAsync(businessId);
            if (business == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Business not found"
                };
            }
            var active_discount = await _context.UserDiscounts
                .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.BusinessId == businessId && ud.EndDate > DateTime.UtcNow && ud.StartDate <= DateTime.UtcNow);
            if (active_discount == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "No active discount found for this user and business"
                };
            }
            active_discount.EndDate = DateTime.UtcNow.AddSeconds(-1);
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Discount removed successfully"
            };
        }
        public async Task<UserDiscountGetDto> GetUserDiscountAsync(string userId, Guid businessId)
        {
            var user = await _usermanager.FindByIdAsync(userId);
            if (user == null)
            {
                return null;

            }
            var business = await _businessRepo.GetByIdAsync(businessId);
            if (business == null)
            {
                return null;
            }
            var discount = await _context.UserDiscounts
                .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.BusinessId == businessId && ud.EndDate > DateTime.UtcNow && ud.StartDate <= DateTime.UtcNow);
            if (discount == null)
            {
                return new UserDiscountGetDto
                {
                    UserId = userId,
                    BusinessId = businessId,
                    Percent = 0,
                    IsActive = false
                };
            }
            return new UserDiscountGetDto
            {
                UserId = userId,
                BusinessId = businessId,
                Percent = discount.Percent,
                StartDate = discount.StartDate,
                EndDate = discount.EndDate,
                IsActive = true
            };
        }
    }
}
