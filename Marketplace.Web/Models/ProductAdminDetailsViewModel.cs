using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class ProductAdminDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public List<string> AdditionalImageUrls { get; set; } = new();
        public ProductStatus Status { get; set; }
        public string? ModerationReason { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
