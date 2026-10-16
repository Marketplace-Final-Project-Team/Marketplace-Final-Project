namespace Marketplace.BLL.DTOs
{
    public class AddressDto
    {
        public Guid Id { get; set; }
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
