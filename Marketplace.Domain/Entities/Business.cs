using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class Business: BaseEntity
    {
        public string OwnerId { get; set; } = string.Empty;
        public User Owner { get; set; } = null!;

        public string StoreName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;

        public decimal Balance { get; set; } = 0;
        public decimal CommissionPercent {  get; set; } = 0;

        public BusinessStatus Status { get; set; } = BusinessStatus.Pending;

        public ICollection<BusinessMember> Members { get; set; } = new List<BusinessMember>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
