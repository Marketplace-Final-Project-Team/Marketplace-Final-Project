using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IPromoCodeService
    {
        // Створити новий промокод для магазину.
        Task<ServiceResult> CreateAsync(Guid businessId, string code, int percent, DateTime startDate, DateTime endDate, bool isSingleUse = false);

        // Достроково завершити дію промокоду (не видаляє, щоб зберегти історію застосувань).
        Task<ServiceResult> DeactivateAsync(Guid businessId, Guid promoCodeId);

        // Усі промокоди магазину — для кабінету продавця.
        Task<List<PromoCodeDto>> GetForBusinessAsync(Guid businessId);

        // Активний промокод за текстом коду, якщо він існує і не прострочений.
        Task<PromoCode?> FindActiveAsync(string code);

        // Попередній розрахунок знижки для поточного кошика покупця — для чекауту.
        Task<PromoCodeApplicationDto> PreviewAsync(string userId, string code);
    }
}
