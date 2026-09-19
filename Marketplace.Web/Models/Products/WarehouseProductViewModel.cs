namespace Marketplace.Web.Models.Products
{
    public class WarehouseProductViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int VariantCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
