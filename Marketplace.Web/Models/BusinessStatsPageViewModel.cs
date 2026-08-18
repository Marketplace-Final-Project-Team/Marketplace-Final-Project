using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class BusinessStatsPageViewModel
    {
        public BusinessStatsQueryViewModel Query { get; set; } = new();
        public BusinessStatsByPeriodDto? Stats { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
