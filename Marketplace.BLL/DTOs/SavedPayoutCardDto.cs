namespace Marketplace.BLL.DTOs
{
    public class SavedPayoutCardDto
    {
        public Guid Id { get; set; }
        public string MaskedCardNumber { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}