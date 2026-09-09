using Marketplace.DAL.DTOs;

namespace Marketplace.BLL.DTOs
{
    public class AdminCommissionStatsDto
    {
        public decimal TotalCommissionByPeriod { get; set; }
        public IReadOnlyList<DailyCommissionStatsDto> Daily { get; set; } = new List<DailyCommissionStatsDto>();
        public IReadOnlyList<CommissionForBusinessDto> ByBusiness { get; set; } = new List<CommissionForBusinessDto>();
    }
}
