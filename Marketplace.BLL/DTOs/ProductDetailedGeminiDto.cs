namespace Marketplace.BLL.DTOs
{
    public class ProductDetailedGeminiDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Quantity { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public float AverageRating { get; set; }
    }
}
