namespace Marketplace.BLL.DTOs
{
    public class SalesSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AOV { get; set; }
    }
}
