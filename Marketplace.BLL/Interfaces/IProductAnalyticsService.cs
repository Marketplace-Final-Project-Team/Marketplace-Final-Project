using Marketplace.BLL.DTOs;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IProductAnalyticsService
    {
        Task RecordEventAsync(Guid productId, Guid businessId, ProductEventType type, string? userId);
        Task<BusinessTrafficStatsDto> GetBusinessTrafficStatsAsync(Guid businessId, DateTime startDate, DateTime endDate);
    }
}
