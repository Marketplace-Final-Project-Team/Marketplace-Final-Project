using Marketplace.BLL.DTOs;
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
        public string OwnerEmail { get; set; } = string.Empty;
        public AdultContentStatus AdultStatus { get; set; }
        public string? AdultRejectionReason { get; set; }
        public int TotalProductCount { get; set; }
        public int ActiveProductCount { get; set; }
        public int PendingProductCount { get; set; }
        public int BannedProductCount { get; set; }

        public int TotalOrders { get; set; }
        public int SoldItems { get; set; }
        public decimal Revenue { get; set; }

        public BusinessDiscountDto? BusinessDiscount { get; set; }
        public List<CategoryDiscountDto> CategoryDiscounts { get; set; } = new();
    }
}