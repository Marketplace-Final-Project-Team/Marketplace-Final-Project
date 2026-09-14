using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IProductTranslationService
    {
        Task TranslateAndSaveAsync(Product product);

        Task<(string Name, string Description)> GetLocalizedAsync(Guid productId, string fallbackName, string fallbackDescription, string culture);
    }
}
