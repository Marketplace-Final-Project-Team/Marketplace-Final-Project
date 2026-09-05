namespace Marketplace.BLL.DTOs
{
    public class AdminStatisticsOverviewDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal TotalTurnover { get; set; }
        public decimal TotalMarketplaceRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUnitsSold { get; set; }
        public int ActiveBusinessCount { get; set; }
        public int TotalBusinessCount { get; set; }
        public int TotalProductCount { get; set; }

        public List<BusinessStatisticsDto> Businesses { get; set; } = new();
        public List<PeriodStatisticsDto> ByPeriod { get; set; } = new();
    }

    public class BusinessStatisticsDto
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int ActiveProductCount { get; set; }
        public int OrdersCount { get; set; }
        public int UnitsSold { get; set; }
        public decimal Turnover { get; set; }
        public decimal MarketplaceRevenue { get; set; }
        public decimal AverageOrderValue => OrdersCount == 0 ? 0 : Math.Round(Turnover / OrdersCount, 2);
    }

    public class PeriodStatisticsDto
    {
        public DateTime PeriodStart { get; set; }
        public string Label { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal Turnover { get; set; }
        public decimal MarketplaceRevenue { get; set; }
    }
}
