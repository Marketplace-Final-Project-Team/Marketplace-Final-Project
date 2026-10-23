using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class AddressViewModel
    {
        public Guid Id { get; set; }
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class AddressFormViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Вкажіть країну")]
        [StringLength(100)]
        public string Country { get; set; } = "Україна";

        [Required(ErrorMessage = "Вкажіть місто")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть вулицю, будинок, квартиру")]
        [StringLength(250)]
        public string Street { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }

        public bool IsDefault { get; set; }
    }
}
