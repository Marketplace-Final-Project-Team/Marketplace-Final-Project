using Marketplace.BLL.DTOs;
using Marketplace.DAL.DTOs;

namespace Marketplace.Web.Models.Businesses
{
    public class BusinessPromoCodesPageViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public List<PromoCodeDto> PromoCodes { get; set; } = new();
        public BusinessDiscountDto? BusinessDiscount { get; set; }
        public List<CategoryDiscountDto> CategoryDiscounts { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public List<UserDiscountGetDto> PersonalDiscounts { get; set; } = new();
        public List<CustomerStatsByPeriodDto> Customers { get; set; } = new();
    }
}
