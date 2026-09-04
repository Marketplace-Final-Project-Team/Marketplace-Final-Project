using Marketplace.Domain.Enums;
using AdultContentStatus = Marketplace.Domain.Entities.AdultContentStatus;

namespace Marketplace.Web.Models
{
    public class AdminBusinessDetailsViewModel
    {
        public Guid Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public BusinessStatus Status { get; set; }
        public decimal CommissionPercent { get; set; }
        public DateTime CreatedAt { get; set; }
        public AdultContentStatus AdultStatus { get; set; }
        public string? AdultRejectionReason { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;

        public int TotalProductCount { get; set; }
        public int ActiveProductCount { get; set; }
        public int PendingProductCount { get; set; }
        public int BannedProductCount { get; set; }

        public bool HasActiveDiscount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public DateTime? DiscountEndDate { get; set; }
    }
}