using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IBusinessService
    {
        Task<ServiceResult> CreateBusiness(BusinessCreateDto dto);
        Task<BusinessMemberResult> AddBusinessMember(BusinessMemberCreateDto dto);
        Task<ServiceResult> ApproveMembership(string userId, Guid businessId, string token, BusinessMemberRole role);
        Task<ServiceResult> DenyMembership(string userId, Guid businessId, string token, BusinessMemberRole role);
        Task<DataResult<BusinessStatsByPeriodDto>> GetBusinessStatsByPeriodAsync(Guid businessId, DateTime? startDate, DateTime? endDate);
        Task<DataResult<ProductsStatsByPeriodDto>> GetProductsStatsByPeriodAsnyc(Guid businessId, DateTime? startDate, DateTime? endDate, bool sortDescending = true);
        Task<DataResult<CustomersStatsByPeriodDto>> GetCustomersStatsByPeriodAsync(Guid businessId, DateTime? startDate, DateTime? endDate, bool sortDescending = true);
        Task<decimal> GetBusinessBalance(Guid businessid);
        Task<Business> GetByIdAsync(Guid id);
    }
}
