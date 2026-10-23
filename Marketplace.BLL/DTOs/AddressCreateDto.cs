namespace Marketplace.BLL.DTOs
{
    public class AddressCreateDto
    {
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
