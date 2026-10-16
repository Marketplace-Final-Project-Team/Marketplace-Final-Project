using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IBusinessService
    {
        Task<ServiceResult> CreateBusinessAsync(BusinessCreateDto dto);
        Task<BusinessMemberResult> AddBusinessMemberAsync(BusinessMemberCreateDto dto);
        Task<ServiceResult> ApproveMembershipAsync(string userId, Guid businessId, string token, BusinessMemberRole role);
        Task<ServiceResult> DenyMembershipAsync(string userId, Guid businessId, string token, BusinessMemberRole role);
        Task<DataResult<BusinessStatsByPeriodDto>> GetBusinessStatsByPeriodAsync(Guid businessId, DateTime? startDate, DateTime? endDate);
        Task<DataResult<ProductsStatsByPeriodDto>> GetProductsStatsByPeriodAsnyc(Guid businessId, DateTime? startDate, DateTime? endDate, bool sortDescending = true);
        Task<DataResult<CustomersStatsByPeriodDto>> GetCustomersStatsByPeriodAsync(Guid businessId, DateTime? startDate, DateTime? endDate, bool sortDescending = true);
        Task<ServiceResult> RequestAdultStatusAsync(Guid businessId, string userId);
        Task<decimal> GetBusinessBalance(Guid businessid);
        Task<Business> GetByIdAsync(Guid id);
    }
}