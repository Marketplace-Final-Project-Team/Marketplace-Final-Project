namespace Marketplace.BLL.DTOs.Products
{
    public class StockForecastDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }
        public int UnitsSoldLast30Days { get; set; }
        private double RawDailySales => UnitsSoldLast30Days / 30.0;
        public double AverageDailySales => Math.Round(RawDailySales, 2);
        public int? EstimatedDaysRemaining => RawDailySales > 0
            ? (int)Math.Ceiling(CurrentQuantity / RawDailySales)
            : null;
    }
}
