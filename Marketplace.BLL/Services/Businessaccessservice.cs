using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;

namespace Marketplace.BLL.Services
{
    public class BusinessAccessService : IBusinessAccessService
    {
        private readonly IGenericRepository<BusinessMember> _memberRepository;
        private readonly IGenericRepository<Business> _businessRepository;

        public BusinessAccessService(
            IGenericRepository<BusinessMember> memberRepository,
            IGenericRepository<Business> businessRepository)
        {
            _memberRepository = memberRepository;
            _businessRepository = businessRepository;
        }

        public async Task<IReadOnlyList<Business>> GetAccessibleBusinessesAsync(string userId, params BusinessMemberRole[] allowedRoles)
        {
            var memberships = await _memberRepository.FindAsync(m => m.UserId == userId);

            if (allowedRoles.Length > 0)
                memberships = memberships.Where(m => allowedRoles.Contains(m.Role)).ToList();

            var businessIds = memberships.Select(m => m.BusinessId).Distinct().ToList();
            if (businessIds.Count == 0)
                return new List<Business>();

            return await _businessRepository.FindAsync(b => businessIds.Contains(b.Id));
        }

        public async Task<bool> HasAccessAsync(Guid businessId, string userId, params BusinessMemberRole[] allowedRoles)
        {
            var memberships = await _memberRepository.FindAsync(m => m.BusinessId == businessId && m.UserId == userId);

            if (allowedRoles.Length == 0)
                return memberships.Any();

            return memberships.Any(m => allowedRoles.Contains(m.Role));
        }
    }
}