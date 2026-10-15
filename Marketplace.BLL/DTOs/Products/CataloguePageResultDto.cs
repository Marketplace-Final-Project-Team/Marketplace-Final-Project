using Marketplace.Domain.Entities;

namespace Marketplace.BLL.DTOs.Products
{
    public class CataloguePageItemDto
    {
        public Product Product { get; set; } = null!;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class CataloguePageResultDto
    {
        public IReadOnlyList<CataloguePageItemDto> Items { get; set; } = Array.Empty<CataloguePageItemDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
