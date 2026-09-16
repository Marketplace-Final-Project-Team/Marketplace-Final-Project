namespace Marketplace.BLL.DTOs.Products
{
    public class ReservedProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public int ReservedQuantity { get; set; }
        public int OrderCount { get; set; }
        public int TotalQuantity { get; set; }
        public int AvailableQuantity => Math.Max(0, TotalQuantity - ReservedQuantity);
    }
}
