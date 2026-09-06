using Marketplace.DAL.DTOs;

namespace Marketplace.Web.Models
{
    public class CommissionStatsAdminViewModel
    {
        public decimal TotalCommissionByPeriod { get; set; }
        public IReadOnlyList<DailyCommissionStatsDto> Daily { get; set; } = new List<DailyCommissionStatsDto>();
        public IReadOnlyList<CommissionForBusinessDto> ByBusiness { get; set; } = new List<CommissionForBusinessDto>();
    }
}
