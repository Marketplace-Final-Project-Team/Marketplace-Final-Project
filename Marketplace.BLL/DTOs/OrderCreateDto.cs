using System.ComponentModel.DataAnnotations;

namespace Marketplace.BLL.DTOs
{
    public class OrderCreateDto
    {
        [Required, StringLength(100)]
        public string RecipientLastName { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string RecipientFirstName { get; set; } = string.Empty;
        [Required, RegularExpression(@"^\+380[0-9]{9}$"), StringLength(30)]
        public string RecipientPhone { get; set; } = string.Empty;
        [Required, RegularExpression("^(NovaPoshta|Ukrposhta|Meest)$")]
        public string DeliveryCarrier { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string ShippingCity { get; set; } = string.Empty;
        [Required, StringLength(250)]
        public string ShippingStreet { get; set; } = string.Empty;
        public string? NovaPoshtaCityRef { get; set; }
        public string? NovaPoshtaBranchRef { get; set; }
        [Required, RegularExpression("^(Online|CashOnDelivery)$")]
        public string PaymentMethod { get; set; } = "Online";
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        public string? PromoCode { get; set; }
        //public string? ServerUrl { get; set; }
    }
}
