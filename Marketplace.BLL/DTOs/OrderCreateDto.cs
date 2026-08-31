namespace Marketplace.BLL.DTOs
{
    public class OrderCreateDto
    {
        public Guid AddressId { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        public string? PromoCode { get; set; }
        //public string? ServerUrl { get; set; }
    }
}
