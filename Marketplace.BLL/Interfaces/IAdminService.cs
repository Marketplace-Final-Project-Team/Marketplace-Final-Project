using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<IReadOnlyList<ProductAdminDto>> GetAllProducts();
        Task<IReadOnlyList<UserAdminDto>> GetAllUsers();
        Task<IReadOnlyList<BusinessAdminDto>> GetAllBusinesses();

        Task<ServiceResult> ChangeProductStatus(Guid productId, ProductStatus productStatus);
        Task<ServiceResult> ChangeUserStatus(string userId, UserStatus userStatus);
        Task<ServiceResult> ChangeBusinessStatus(Guid businessId, BusinessStatus businessStatus);
    }
}
