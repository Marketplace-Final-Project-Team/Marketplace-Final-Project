using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class Business : BaseEntity
    {
        public string OwnerId { get; set; } = string.Empty;
        public User Owner { get; set; } = null!;

        public string StoreName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;

        public BusinessStatus Status { get; set; } = BusinessStatus.Pending;

        public AdultContentStatus AdultStatus { get; set; } = AdultContentStatus.None;
        public string? AdultRejectionReason { get; set; }

        public ICollection<BusinessMember> Members { get; set; } = new List<BusinessMember>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public enum AdultContentStatus
    {
        None = 0,       
        Pending = 1,    
        Approved = 2,   
        Rejected = 3    
    }
}
