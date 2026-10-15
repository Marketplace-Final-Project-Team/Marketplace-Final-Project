namespace Marketplace.Web.Models
{
    public class ProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Avatar {  get; set; } = string.Empty;

        public int OrdersCount { get; set; }
        public int AddressesCount { get; set; }
        public int WishlistCount { get; set; }
        public int DiscountsCount { get; set; }

        public List<PersonalDiscountViewModel> PersonalDiscounts { get; set; } = new();
    }
}
