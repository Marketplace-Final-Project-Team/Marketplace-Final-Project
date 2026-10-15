using Marketplace.BLL.Common;
using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IContentModerationService
    {
        Task<ModerationResult> CheckProductContentAsync(Product product);
        Task<ModerationResult> CheckBusinessAsync(Business business);
        Task<ReturnModerationResult> CheckReturnRequestAsync(PostSaleRequest request);
    }
}