using System.ComponentModel.DataAnnotations;
using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models.Businesses
{
    public class BusinessFinanceViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int SoldItems { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue => TotalOrders == 0 ? 0 : Revenue / TotalOrders;
        public List<BusinessOrderListItemViewModel> RecentSales { get; set; } = new();
    }

    public class BusinessPerformanceViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public BusinessStatus Status { get; set; }
        public int TotalOrders { get; set; }
        public int CanceledOrders { get; set; }
        public double CancelRatePercent => TotalOrders == 0 ? 0 : Math.Round(CanceledOrders * 100d / TotalOrders, 1);
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int ProductCount { get; set; }
    }

    public class BusinessGrowthViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
    }

    public class BusinessIntegrationsViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
    }

    public class BusinessSettingsViewModel
    {
        public Guid BusinessId { get; set; }

        [Required(ErrorMessage = "Вкажіть назву магазину")]
        [StringLength(120)]
        public string StoreName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public AdultContentStatus AdultStatus { get; set; }
    }
}
