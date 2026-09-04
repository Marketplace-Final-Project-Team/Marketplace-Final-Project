using Marketplace.Domain.Common;
namespace Marketplace.Domain.Entities
{
    public class OrderItem: BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal CommissionAmount { get; set; }
        public bool IsReturned { get; set; } = false;
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}