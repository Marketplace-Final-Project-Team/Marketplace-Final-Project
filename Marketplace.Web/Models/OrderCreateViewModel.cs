using System.ComponentModel.DataAnnotations;
using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models;

public class OrderCreateViewModel : IValidatableObject
{
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }

    [Required(ErrorMessage = "Вкажіть прізвище"), StringLength(100)]
    public string RecipientLastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть ім’я"), StringLength(100)]
    public string RecipientFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть телефон"), StringLength(30)]
    [RegularExpression(@"^\+380[0-9]{9}$", ErrorMessage = "Введіть 9 цифр номера телефону після +380")]
    public string RecipientPhone { get; set; } = string.Empty;

    public List<CartItemDto> Items { get; set; } = new();
    public decimal Total => Items.Sum(item => item.LineTotal);

    public List<CartSellerGroupViewModel> SellerGroups { get; set; } = new();
    public bool HasMultipleSellers => SellerGroups.Count > 1;

    [Required, RegularExpression("^(NovaPoshta|Ukrposhta|Meest)$", ErrorMessage = "Оберіть спосіб доставки")]
    public string DeliveryCarrier { get; set; } = DeliveryCarriers.NovaPoshta;

    // Only populated/required when DeliveryCarrier == NovaPoshta.
    public string? NovaPoshtaCityRef { get; set; }
    [StringLength(100)]
    public string? NovaPoshtaCityName { get; set; }
    public string? NovaPoshtaBranchRef { get; set; }
    [StringLength(250)]
    public string? NovaPoshtaBranchName { get; set; }

    // Address from step 2 for Ukrposhta or Meest.
    [StringLength(100)]
    public string? DeliveryCity { get; set; }

    [StringLength(250)]
    public string? DeliveryStreet { get; set; }

    [Required, RegularExpression("^(Online|CashOnDelivery)$", ErrorMessage = "Оберіть спосіб оплати")]
    public string PaymentMethod { get; set; } = PaymentMethods.Online;

    [StringLength(30)]
    public string? PromoCode { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DeliveryCarrier == DeliveryCarriers.NovaPoshta
            && (string.IsNullOrWhiteSpace(NovaPoshtaCityRef) || string.IsNullOrWhiteSpace(NovaPoshtaBranchRef)
                || string.IsNullOrWhiteSpace(NovaPoshtaCityName) || string.IsNullOrWhiteSpace(NovaPoshtaBranchName)))
        {
            yield return new ValidationResult("Оберіть місто та відділення Нової пошти", new[] { nameof(NovaPoshtaBranchRef) });
        }

        if (DeliveryCarrier is DeliveryCarriers.Ukrposhta or DeliveryCarriers.Meest)
        {
            if (string.IsNullOrWhiteSpace(DeliveryCity))
            {
                yield return new ValidationResult("Вкажіть місто доставки", new[] { nameof(DeliveryCity) });
            }

            if (string.IsNullOrWhiteSpace(DeliveryStreet))
            {
                yield return new ValidationResult("Вкажіть адресу доставки", new[] { nameof(DeliveryStreet) });
            }
        }
    }

    public List<AddressViewModel> SavedAddresses { get; set; } = new();
}

public static class DeliveryCarriers
{
    public const string NovaPoshta = "NovaPoshta";
    public const string Ukrposhta = "Ukrposhta";
    public const string Meest = "Meest";

    public static string Label(string carrier) => carrier switch
    {
        NovaPoshta => "Нова пошта",
        Ukrposhta => "Укрпошта",
        Meest => "Meest Express",
        _ => carrier
    };
}

public static class PaymentMethods
{
    public const string Online = "Online";
    public const string CashOnDelivery = "CashOnDelivery";

    public static string Label(string method) => method switch
    {
        Online => "LiqPay (онлайн)",
        CashOnDelivery => "Оплата при отриманні",
        _ => method
    };
}
