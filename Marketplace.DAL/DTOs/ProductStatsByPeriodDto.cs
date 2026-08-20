namespace Marketplace.DAL.DTOs
{
    public class ProductStatsByPeriodDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TotalSales { get; set; }
    }
}
