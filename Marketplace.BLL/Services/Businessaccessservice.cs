using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;

namespace Marketplace.BLL.Services
{
    public class BusinessAccessService : IBusinessAccessService
    {
        private readonly IGenericRepository<BusinessMember> _memberRepository;
        private readonly IBusinessRepository _businessRepository;

        public BusinessAccessService(
            IGenericRepository<BusinessMember> memberRepository,
            IBusinessRepository businessRepository)
        {
            _memberRepository = memberRepository;
            _businessRepository = businessRepository;
        }

        public async Task<IReadOnlyList<Business>> GetAccessibleBusinessesAsync(string userId, params BusinessMemberRole[] allowedRoles)
        {
            var result = new Dictionary<Guid, Business>();

            if (allowedRoles.Length == 0 || allowedRoles.Contains(BusinessMemberRole.Owner))
            {
                var ownedBusinesses = await _businessRepository.FindAsync(b => b.OwnerId == userId);
                foreach (var business in ownedBusinesses)
                {
                    result[business.Id] = business;
                }
            }

            var memberships = await _memberRepository.FindAsync(m => m.UserId == userId);

            if (allowedRoles.Length > 0)
                memberships = memberships.Where(m => allowedRoles.Contains(m.Role)).ToList();

            var businessIds = memberships.Select(m => m.BusinessId).Distinct().ToList();
            if (businessIds.Count > 0)
            {
                var memberBusinesses = await _businessRepository.FindAsync(b => businessIds.Contains(b.Id));
                foreach (var business in memberBusinesses)
                {
                    result[business.Id] = business;
                }
            }

            return result.Values.OrderBy(b => b.StoreName).ToList();
        }

        public async Task<bool> HasAccessAsync(Guid businessId, string userId, params BusinessMemberRole[] allowedRoles)
        {
            if (allowedRoles.Length == 0 || allowedRoles.Contains(BusinessMemberRole.Owner))
            {
                var isOwner = await _businessRepository.ExistsAsync(
                    b => b.Id == businessId && b.OwnerId == userId);
                if (isOwner)
                {
                    return true;
                }
            }

            var memberships = await _memberRepository.FindAsync(m => m.BusinessId == businessId && m.UserId == userId);

            if (allowedRoles.Length == 0)
                return memberships.Any();

            return memberships.Any(m => allowedRoles.Contains(m.Role));
        }
    }
}
