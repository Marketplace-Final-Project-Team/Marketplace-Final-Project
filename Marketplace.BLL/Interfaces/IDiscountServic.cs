using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IDiscountService
    {
        Task SetDiscountAsync(Guid productId, decimal percent, DateTime startDate, DateTime endDate);

        Task RemoveDiscountAsync(Guid productId);

        Task<ServiceResult> SetBusinessDiscount(Guid businessId, decimal discountPercent, DateTime discountStartDate, DateTime discountEndDate);
        Task<ServiceResult> RemoveBusinessDiscount(Guid businessId);
        Task<BusinessDiscountDto> GetBusinessDiscount(Guid businessId);
    }
}