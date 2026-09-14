using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class PromoCodeService : IPromoCodeService
    {
        private readonly AppDbContext _context;
        private readonly ISystemSettingsService _settingsService;

        public PromoCodeService(AppDbContext context, ISystemSettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<ServiceResult> CreateAsync(Guid businessId, string code, int percent, DateTime startDate, DateTime endDate, bool isSingleUse = false)
        {
            var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(normalizedCode) || normalizedCode.Length < 3 || normalizedCode.Length > 30)
            {
                return new ServiceResult { Succeeded = false, Message = "Код має містити від 3 до 30 символів." };
            }

            var maxPromoCodePercent = await _settingsService.GetIntAsync("BusinessRules", "MaxPromoCodePercent", 90);
            if (percent < 1 || percent > maxPromoCodePercent)
            {
                return new ServiceResult { Succeeded = false, Message = $"Знижка має бути від 1% до {maxPromoCodePercent}%." };
            }

            if (endDate <= startDate)
            {
                return new ServiceResult { Succeeded = false, Message = "Дата закінчення має бути пізніше дати початку." };
            }

            var codeTaken = await _context.PromoCodes.AnyAsync(p => p.Code == normalizedCode);
            if (codeTaken)
            {
                return new ServiceResult { Succeeded = false, Message = "Такий код уже використовується - оберіть інший." };
            }

            var maxActivePromoCodes = await _settingsService.GetIntAsync("BusinessRules", "MaxActivePromoCodesPerBusiness", 20);
            var now = DateTime.UtcNow;
            var activePromoCodeCount = await _context.PromoCodes
                .CountAsync(p => p.BusinessId == businessId && p.StartDate <= now && p.EndDate >= now);
            if (activePromoCodeCount >= maxActivePromoCodes)
            {
                return new ServiceResult { Succeeded = false, Message = $"Досягнуто максимальної кількості активних промокодів ({maxActivePromoCodes})." };
            }

            var promoCode = new PromoCode
            {
                BusinessId = businessId,
                Code = normalizedCode,
                Percent = percent,
                StartDate = startDate,
                EndDate = endDate,
                IsSingleUse = isSingleUse
            };

            _context.PromoCodes.Add(promoCode);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Промокод створено.", EntityId = promoCode.Id };
        }

        public async Task<ServiceResult> DeactivateAsync(Guid businessId, Guid promoCodeId)
        {
            var promoCode = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Id == promoCodeId && p.BusinessId == businessId);

            if (promoCode is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Промокод не знайдено." };
            }

            promoCode.EndDate = DateTime.UtcNow.AddSeconds(-1);
            promoCode.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Промокод деактивовано." };
        }

        public async Task<List<PromoCodeDto>> GetForBusinessAsync(Guid businessId)
        {
            var promoCodes = await _context.PromoCodes
                .Where(p => p.BusinessId == businessId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return promoCodes.Select(p => new PromoCodeDto
            {
                Id = p.Id,
                Code = p.Code,
                Percent = p.Percent,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsSingleUse = p.IsSingleUse,
                IsUsed = p.IsUsed,
                IsActive = p.IsActive
            }).ToList();
        }

        public async Task<PromoCode?> FindActiveAsync(string code)
        {
            var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return null;
            }

            var now = DateTime.UtcNow;
            return await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == normalizedCode && p.StartDate <= now && p.EndDate >= now
                    && !(p.IsSingleUse && p.IsUsed));
        }

        public async Task<PromoCodeApplicationDto> PreviewAsync(string userId, string code)
        {
            var promoCode = await _context.PromoCodes
                .Include(p => p.Business)
                .FirstOrDefaultAsync(p => p.Code == (code ?? string.Empty).Trim().ToUpperInvariant());

            if (promoCode is null || !promoCode.IsActive)
            {
                return new PromoCodeApplicationDto { Succeeded = false, Message = "Промокод недійсний або термін його дії минув." };
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var matchingSubtotal = cart?.Items
                .Where(i => i.Product.BusinessId == promoCode.BusinessId)
                .Sum(i => i.Product.Price * i.Quantity) ?? 0m;

            if (matchingSubtotal <= 0)
            {
                return new PromoCodeApplicationDto
                {
                    Succeeded = false,
                    Message = $"У кошику немає товарів магазину «{promoCode.Business.StoreName}», для яких діє цей код."
                };
            }

            return new PromoCodeApplicationDto
            {
                Succeeded = true,
                Message = $"Знижку {promoCode.Percent}% застосовано до товарів «{promoCode.Business.StoreName}».",
                Code = promoCode.Code,
                Percent = promoCode.Percent,
                BusinessId = promoCode.BusinessId,
                StoreName = promoCode.Business.StoreName,
                DiscountAmount = Math.Round(matchingSubtotal * promoCode.Percent / 100m, 2)
            };
        }
    }
}
