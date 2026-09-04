using Marketplace.Domain.Entities;

namespace Marketplace.Web.Models
{
    public class DiscountManageViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductMainImageUrl { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public Discount? ActiveDiscount { get; set; }
    }
}