using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models.Businesses
{
    public class BusinessPromoCodesPageViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public List<PromoCodeDto> PromoCodes { get; set; } = new();
        public BusinessDiscountDto? BusinessDiscount { get; set; }
    }
}
