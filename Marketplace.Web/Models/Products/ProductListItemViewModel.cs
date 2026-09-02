namespace Marketplace.Web.Models.Products
{
    public class ProductListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? ModerationReason { get; set; }
    }
}