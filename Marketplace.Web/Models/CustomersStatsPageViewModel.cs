using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class CustomersStatsPageViewModel
    {
        public CustomersStatsQueryViewModel Query { get; set; } = new();
        public CustomersStatsByPeriodDto? Stats { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
