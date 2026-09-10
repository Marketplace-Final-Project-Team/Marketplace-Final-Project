using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly AppDbContext _context;
        private readonly IBusinessRepository _businessRepo;
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly ISystemSettingsService _settingsService;

        public DiscountService(AppDbContext context, IBusinessRepository businessRepo, IGenericRepository<Category> categoryRepo, ISystemSettingsService settingsService)
        {
            _context = context;
            _businessRepo = businessRepo;
            _categoryRepo = categoryRepo;
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

            var productDiscounts = await _context.Discounts
                .Where(d => d.ProductId == productId && d.DiscountType == DiscountType.Product && d.EndDate > DateTime.UtcNow)
                .ToListAsync();

            foreach (var old in productDiscounts)
            {
                old.EndDate = DateTime.UtcNow.AddSeconds(-1);
            }
            
            



            var discount = new Discount
            {
                ProductId = productId,
                Percent = percent,
                StartDate = startDate,
                EndDate = endDate,
                DiscountType = DiscountType.Product,
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveDiscountAsync(Guid productId)
        {
            var activeDiscounts = await _context.Discounts
                .Where(d => d.ProductId == productId && d.DiscountType == DiscountType.Product && d.EndDate > DateTime.UtcNow)
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
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.BusinessId == businessId && d.DiscountType == DiscountType.Business && d.EndDate > DateTime.UtcNow);
            
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
                active_discount.EndDate = DateTime.UtcNow.AddSeconds(-1);
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
            

            var discount = new Discount
            {
                BusinessId = businessId,
                Percent = percent,
                StartDate = startDate,
                EndDate = endDate,
                DiscountType = DiscountType.Business
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
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.BusinessId == businessId && d.DiscountType == DiscountType.Business && d.EndDate >= DateTime.UtcNow);
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
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.BusinessId == businessId && d.DiscountType == DiscountType.Business && d.EndDate >= DateTime.UtcNow);
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
        public async Task<ServiceResult> SetCategoryDiscount(Guid categoryId, Guid businessId, decimal percent, DateTime startDate, DateTime endDate)
        {

            
            var category = await _categoryRepo.GetByIdAsync(categoryId);
            

            if (category == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Категорія не знайдена"
                };
            }
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.CategoryId == categoryId && d.BusinessId == businessId && d.DiscountType == DiscountType.Category && d.EndDate > DateTime.UtcNow);
            if (active_discount != null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Неможливо встановити знижку для категорії, поки існує активна знижка для цієї категорії"
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
            


                var discount = new Discount
            {
                CategoryId = categoryId,
                BusinessId = businessId,
                Percent = percent,
                StartDate = startDate,
                EndDate = endDate,
                DiscountType = DiscountType.Category
                };


            await _context.Discounts.AddAsync(discount);
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Знижка успішно встановлена"

            };
        }
        public async Task<ServiceResult> RemoveCategoryDiscount(Guid categoryId, Guid businessId)
        {
            var category = await _categoryRepo.GetByIdAsync(categoryId);
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.CategoryId == categoryId && d.BusinessId == businessId && d.DiscountType == DiscountType.Category && d.EndDate >= DateTime.UtcNow);
            if (category == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Категорія не знайдена"
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
        public async Task<CategoryDiscountDto> GetCategoryDiscount(Guid categoryId, Guid businessId)
        {
            var category = await _categoryRepo.GetByIdAsync(categoryId);
            if (category == null)
            {
                return null;
            }
            var active_discount = await _context.Discounts.FirstOrDefaultAsync(d => d.CategoryId == categoryId && d.BusinessId == businessId && d.DiscountType == DiscountType.Category && d.EndDate >= DateTime.UtcNow);
            

            if (active_discount != null && active_discount.IsActive)
            {
                return new CategoryDiscountDto
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    BusinessId = businessId,
                    DiscountPercent = active_discount.Percent,
                    StartDate = active_discount.StartDate,
                    EndDate = active_discount.EndDate,
                    IsActive = true
                };

            }


            return new CategoryDiscountDto
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                BusinessId = businessId,
                DiscountPercent = 0,
                IsActive = false
            };


        }

        public async Task<List<CategoryDiscountDto>> GetActiveCategoryDiscountsForBusiness(Guid businessId)
        {
            return await _context.Discounts
                .Where(d => d.BusinessId == businessId && d.DiscountType == DiscountType.Category && d.EndDate >= DateTime.UtcNow && d.CategoryId != null)
                .Include(d => d.Category)
                .Select(d => new CategoryDiscountDto
                {
                    CategoryId = d.CategoryId!.Value,
                    CategoryName = d.Category!.Name,
                    BusinessId = businessId,
                    DiscountPercent = d.Percent,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    IsActive = d.IsActive
                })
                .ToListAsync();
        }
    }
}