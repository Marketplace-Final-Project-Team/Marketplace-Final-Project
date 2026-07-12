using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class BusinessMember : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BusinessId { get; set; }
        public Business Business { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public BusinessMemberRole Role { get; set; }
    }
}