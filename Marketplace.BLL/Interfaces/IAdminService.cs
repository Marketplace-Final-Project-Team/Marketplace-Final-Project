using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Marketplace.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<PagedResult<ProductAdminDto>> GetProductsAsync(ProductAdminQueryDto query);
        Task<PagedResult<UserAdminDto>> GetUsersAsync(UserAdminQueryDto query);
        Task<PagedResult<BusinessAdminDto>> GetBusinessesAsync(BusinessAdminQueryParams query);

        Task<ServiceResult> ChangeProductStatus(Guid productId, ProductStatus productStatus);
        Task<ServiceResult> ChangeUserStatus(string userId, UserStatus userStatus);
        Task<ServiceResult> ChangeBusinessStatus(Guid businessId, BusinessStatus businessStatus);
    }
}
