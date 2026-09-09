using Marketplace.DAL.DTOs;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;

namespace Marketplace.DAL.Interfaces
{
    public interface IOrderRepository: IGenericRepository<Order>
    {
        Task<List<DailySalesStatsDto>> GetBusinessStatByPeriodAsync(Guid businessId, DateTime startDate, DateTime endDate);
        Task<List<ProductStatsByPeriodDto>> GetProductsStatsByPeriodAsync(Guid businessId, DateTime startDate, DateTime endDate);
        Task<List<CustomerStatsByPeriodDto>> GetCustomersStatsByPeriodAsync(Guid businessId, DateTime startDate, DateTime endDate);
        Task<List<DailyCommissionStatsDto>> GetCommissionPerDayByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<List<CommissionForBusinessDto>> GetCommissionForBusinessByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalCommissionAsync();
    }
}
