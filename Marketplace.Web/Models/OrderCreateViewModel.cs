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

    public bool IsUsingSavedAddress => SelectedAddressId.HasValue && SelectedAddressId.Value != Guid.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsUsingSavedAddress)
        {
            yield break;
        }

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
}
