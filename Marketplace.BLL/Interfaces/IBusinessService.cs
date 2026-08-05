using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public  interface IBusinessService
    {
        Task<ServiceResult> CreateBusiness(BusinessCreateDto dto);
        Task<BusinessMemberResult> AddBusinessMember(BusinessMemberCreateDto dto);
        Task<ServiceResult> ApproveMembership(string userId, Guid businessId, string token, BusinessMemberRole role);
        Task<ServiceResult> DenyMembership(string userId, Guid businessId, string token, BusinessMemberRole role);
    }
}
