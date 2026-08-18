using Marketplace.DAL.DTOs;

namespace Marketplace.BLL.DTOs
{
    public class CustomersStatsByPeriodDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<CustomerStatsByPeriodDto> Stats { get; set; } = new List<CustomerStatsByPeriodDto>();
        public int UniqueCustomerCount { get; set; }
    }
}
