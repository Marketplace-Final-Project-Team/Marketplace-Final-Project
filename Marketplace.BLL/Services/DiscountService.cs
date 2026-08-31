using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly AppDbContext _context;
        private readonly IBusinessRepository _businessRepo;
        private readonly ISystemSettingsService _settingsService;

        public DiscountService(AppDbContext context, IBusinessRepository businessRepo, ISystemSettingsService settingsService)
        {
            _context = context;
            _businessRepo = businessRepo;
            _settingsService = settingsService;
        }

        public async Task SetDiscountAsync(Guid productId, decimal percent, DateTime startDate, DateTime endDate)
        {
            var maxDiscountPercent = await _settingsService.GetIntAsync("BusinessRules", "MaxDiscountPercent", 90);
            if (percent < 1 || percent > maxDiscountPercent)
                throw new ArgumentOutOfRangeException(nameof(percent), $"Знижка має бути від 1% до {maxDiscountPercent}%.");

            if (endDate <= startDate)
                throw new ArgumentException("Дата закінчення має бути пізніше дати початку.");
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new ArgumentException("Продукт не знайдено");
            }

            var existingDiscounts = await _context.Discounts
                .Where(d => d.ProductId == productId)
                .ToListAsync();

            foreach (var old in existingDiscounts)
            {
                old.EndDate = DateTime.UtcNow.AddSeconds(-1);
            }

            var discount = new Discount
            {
                ProductId = productId,
                Percent = percent,
                StartDate = startDate,
                EndDate = endDate
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveDiscountAsync(Guid productId)
        {
            var activeDiscounts = await _context.Discounts
                .Where(d => d.ProductId == productId && d.EndDate > DateTime.UtcNow)
                .ToListAsync();

            foreach (var d in activeDiscounts)
            {
                d.EndDate = DateTime.UtcNow.AddSeconds(-1);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ServiceResult> SetBusinessDiscount(Guid businessId, decimal percent, DateTime startDate, DateTime endDate)
        {
            

            var business = await _businessRepo.GetByIdAsync(businessId);
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.BusinessId == businessId && d.EndDate > DateTime.UtcNow);
            
            if (business == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Бізнес не знайдено"
                };
            }
            if (active_discount != null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Неможливо встановити знижку для бізнесу, поки існує активна знижка для цього бізнесу"
                };
            }
            var maxDiscountPercent = await _settingsService.GetIntAsync("BusinessRules", "MaxDiscountPercent", 90);
            if (percent < 0 || percent > maxDiscountPercent)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = $"Знижка повинна бути в межах від 0 до {maxDiscountPercent}"
                };
            }
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
            var productDiscounts = await _context.Discounts
                .Where(d => d.Product.BusinessId == businessId && d.EndDate > DateTime.UtcNow)
                .ToListAsync();
            foreach( var pd in productDiscounts)
            {
                 pd.EndDate = DateTime.UtcNow.AddSeconds(-1);
            }

            var discount = new Discount
            {
                BusinessId = businessId,
                Percent = percent,
                StartDate = startDate,
                EndDate = endDate
            };
            
            
            await _context.Discounts.AddAsync(discount);
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Знижка успішно встановлена"

            };
        }
        public async Task<ServiceResult> RemoveBusinessDiscount(Guid businessId)
        {
            var business = await _businessRepo.GetByIdAsync(businessId);
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.BusinessId == businessId && d.EndDate >= DateTime.UtcNow);
            if (business == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Бізнес не знайдено"
                };

            }
            if (active_discount == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Немає активної знижки для видалення"
                };

            }
            active_discount.EndDate = DateTime.UtcNow.AddSeconds(-1);
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Знижка успішно видалена"
            };

        }
        public async Task<BusinessDiscountDto> GetBusinessDiscount(Guid businessId)
        {
            var business = await _businessRepo.GetByIdAsync(businessId);
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.BusinessId == businessId && d.EndDate >= DateTime.UtcNow);
            if (business == null)
            {
                return null;
            }
            
            if (active_discount != null && active_discount.IsActive)
            {
                return new BusinessDiscountDto
                {
                    BusinessId = business.Id,
                    DiscountPercent = active_discount.Percent,
                    StartDate = active_discount.StartDate,
                    EndDate = active_discount.EndDate,
                    IsActive = true
                };

            }


            return new BusinessDiscountDto
            {
                BusinessId = businessId,
                DiscountPercent = 0,
                IsActive = false
            };


        }
    }
}