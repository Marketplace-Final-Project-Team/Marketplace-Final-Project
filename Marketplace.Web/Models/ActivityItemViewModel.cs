using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class ActivityItemViewModel
    {
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
