using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class AdminStatisticsFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AdminStatisticsPageViewModel
    {
        public AdminStatisticsFilterViewModel Filter { get; set; } = new();
        public AdminStatisticsOverviewDto Overview { get; set; } = new();
    }
}
