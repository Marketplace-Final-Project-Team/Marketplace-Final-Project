namespace Marketplace.DAL.DTOs
{
    public class CustomerStatsByPeriodDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int PurchaseCount { get; set; }

    }
}
