namespace Marketplace.BLL.DTOs
{
    public class ProductGeminiDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public float AverageRating { get; set; }
    }
}
