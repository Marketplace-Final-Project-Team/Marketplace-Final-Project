namespace Marketplace.DAL.DTOs
{
    public class DailySalesStatsDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}
