using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Marketplace.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<PagedResult<ProductAdminDto>> GetFilteredProductsAsync(ProductAdminQueryDto query);
        Task<PagedResult<UserAdminDto>> GetFilteredUsersAsync(UserAdminQueryDto query);
        Task<PagedResult<BusinessAdminDto>> GetFilteredBusinessesAsync(BusinessAdminQueryParams query);

        Task<IReadOnlyList<ProductAdminDto>> GetProductsAsync();
        IReadOnlyList<UserAdminDto> GetUsers();
        Task<IReadOnlyList<BusinessAdminDto>> GetBusinessesAsync();


        Task<ServiceResult> ChangeProductStatus(Guid productId, ProductStatus productStatus);
        Task<ServiceResult> ChangeUserStatus(string userId, UserStatus userStatus);
        Task<ServiceResult> ChangeBusinessStatus(Guid businessId, BusinessStatus businessStatus);
        Task<IReadOnlyList<BusinessAdminDto>> GetPendingAdultRequestsAsync();
        Task<ServiceResult> ApproveAdultStatusAsync(Guid businessId);
        Task<ServiceResult> RejectAdultStatusAsync(Guid businessId, string reason);
        Task<ServiceResult> ChangeBusinessComission(Guid businessId, decimal commissionPercent);

        Task<AdminStatisticsOverviewDto> GetStatisticsAsync(DateTime? startDate, DateTime? endDate);
    }
}