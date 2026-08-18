using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class ProductsStatsPageViewModel
    {
        public ProductsStatsQueryViewModel Query { get; set; } = new();
        public ProductsStatsByPeriodDto? Stats { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
