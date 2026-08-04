using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IBusinessAccessService
    {
     
        Task<IReadOnlyList<Business>> GetAccessibleBusinessesAsync(string userId, params BusinessMemberRole[] allowedRoles);

        Task<bool> HasAccessAsync(Guid businessId, string userId, params BusinessMemberRole[] allowedRoles);
    }
}