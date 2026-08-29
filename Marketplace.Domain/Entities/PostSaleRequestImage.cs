using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class PostSaleRequestImage : BaseEntity
    {
        public string ImageUrl { get; set; } = string.Empty;

        public Guid PostSaleRequestId { get; set; }
        public PostSaleRequest PostSaleRequest { get; set; } = null!;
    }
}
