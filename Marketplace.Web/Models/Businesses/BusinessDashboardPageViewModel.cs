namespace Marketplace.Web.Models.Businesses
{
    public class BusinessDashboardFilterViewModel
    {
        public Guid BusinessId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class BusinessDashboardChartPointViewModel
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class BusinessDashboardTopProductViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int UnitsSold { get; set; }
        public int Views { get; set; }
        public double ConversionRatePercent => Views == 0 ? 0 : Math.Round(UnitsSold * 100d / Views, 1);
    }

    public class BusinessDashboardTopCustomerViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int PurchaseCount { get; set; }
    }

    public class BusinessDashboardCategoryStatViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int UnitsSold { get; set; }
    }

    public class BusinessDashboardActivityItemViewModel
    {
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class BusinessDashboardPageViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public BusinessDashboardFilterViewModel Filter { get; set; } = new();

        public List<BusinessDashboardChartPointViewModel> Chart { get; set; } = new();
        public decimal PeriodRevenue { get; set; }
        public int PeriodOrders { get; set; }
        public double PeriodAverageOrderValue { get; set; }

        public double? RevenueDeltaPercent { get; set; }
        public double? OrdersDeltaPercent { get; set; }
        public double? AverageOrderValueDeltaPercent { get; set; }

        public decimal Balance { get; set; }
        public decimal AllTimeRevenue { get; set; }
        public int AllTimeOrders { get; set; }
        public int AllTimeSoldItems { get; set; }
        public int AwaitingOrders { get; set; }

        public int TotalProductCount { get; set; }
        public int ActiveProductCount { get; set; }
        public int PendingProductCount { get; set; }
        public int OutOfStockProductCount { get; set; }
        public int LowStockProductCount { get; set; }

        public int TotalViews { get; set; }
        public int TotalCartAdds { get; set; }
        public int TotalWishlistAdds { get; set; }
        public double ConversionRatePercent => TotalViews == 0 ? 0 : Math.Round(PeriodOrders * 100d / TotalViews, 1);

        public List<BusinessDashboardTopProductViewModel> TopProducts { get; set; } = new();
        public List<BusinessDashboardTopCustomerViewModel> TopCustomers { get; set; } = new();
        public List<BusinessDashboardCategoryStatViewModel> CategoryStats { get; set; } = new();
        public List<BusinessDashboardActivityItemViewModel> RecentActivity { get; set; } = new();
    }
}
