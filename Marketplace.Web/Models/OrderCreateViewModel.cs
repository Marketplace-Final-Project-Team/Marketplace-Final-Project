using System.ComponentModel.DataAnnotations;
using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models;

public class OrderCreateViewModel : IValidatableObject
{
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }

    // Empty/null => the buyer is entering a new address in Country/City/Street below instead
    // of picking one of SavedAddresses.
    public Guid? SelectedAddressId { get; set; }

    [StringLength(100)]
    public string? Country { get; set; } = "Україна";

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(250)]
    public string? Street { get; set; }

    public List<AddressViewModel> SavedAddresses { get; set; } = new();

    public List<CartItemDto> Items { get; set; } = new();
    public decimal Total => Items.Sum(item => item.LineTotal);

    public List<CartSellerGroupViewModel> SellerGroups { get; set; } = new();
    public bool HasMultipleSellers => SellerGroups.Count > 1;

    public bool IsUsingSavedAddress => SelectedAddressId.HasValue && SelectedAddressId.Value != Guid.Empty;

    public string DeliveryCarrier { get; set; } = DeliveryCarriers.NovaPoshta;

    // Only populated/required when DeliveryCarrier == NovaPoshta.
    public string? NovaPoshtaCityRef { get; set; }
    public string? NovaPoshtaCityName { get; set; }
    public string? NovaPoshtaBranchRef { get; set; }
    public string? NovaPoshtaBranchName { get; set; }

    // Only populated/required when DeliveryCarrier is Ukrposhta or Meest — a delivery
    // address that can differ from the billing address entered in step 1 (e.g. gifting
    // an order to someone else).
    [StringLength(100)]
    public string? DeliveryCity { get; set; }

    [StringLength(250)]
    public string? DeliveryStreet { get; set; }

    public string PaymentMethod { get; set; } = PaymentMethods.Online;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsUsingSavedAddress)
        {
            if (string.IsNullOrWhiteSpace(Country))
            {
                yield return new ValidationResult("Вкажіть країну", new[] { nameof(Country) });
            }

            if (string.IsNullOrWhiteSpace(City))
            {
                yield return new ValidationResult("Вкажіть місто", new[] { nameof(City) });
            }

            if (string.IsNullOrWhiteSpace(Street))
            {
                yield return new ValidationResult("Вкажіть адресу доставки", new[] { nameof(Street) });
            }
        }

        if (DeliveryCarrier == DeliveryCarriers.NovaPoshta
            && (string.IsNullOrWhiteSpace(NovaPoshtaCityRef) || string.IsNullOrWhiteSpace(NovaPoshtaBranchRef)))
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
