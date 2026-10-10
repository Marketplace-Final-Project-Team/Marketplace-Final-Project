namespace Marketplace.BLL.DTOs
{
    public class DiscountGeminiDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
