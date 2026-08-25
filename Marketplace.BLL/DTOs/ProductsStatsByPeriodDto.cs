using Marketplace.DAL.DTOs;

namespace Marketplace.BLL.DTOs
{
    public class ProductsStatsByPeriodDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ProductStatsByPeriodDto> Stats { get; set; } = new List<ProductStatsByPeriodDto>();
    }
}
