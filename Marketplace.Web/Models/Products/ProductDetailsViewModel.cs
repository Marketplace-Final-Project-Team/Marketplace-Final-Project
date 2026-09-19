namespace Marketplace.Web.Models.Products
{
    public class ProductDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Sku { get; set; }
        public List<ProductVariantInputModel> Variants { get; set; } = new();
        public string MainImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? ModerationReason { get; set; }
        public List<string> AdditionalImageUrls { get; set; } = new();
    }
}