namespace Marketplace.BLL.DTOs
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string? VariantSize { get; set; }
        public decimal LineTotal => Price * Quantity;
    }
}
