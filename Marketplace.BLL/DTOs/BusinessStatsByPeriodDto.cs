using Marketplace.DAL.DTOs;

namespace Marketplace.BLL.DTOs
{
    public class BusinessStatsByPeriodDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DailySalesStatsDto> Stats { get; set; } = new List<DailySalesStatsDto>();
        public decimal TotalRevenue { get; set; }
        public float AOV = 0; //AOV - average order value
    }
}
