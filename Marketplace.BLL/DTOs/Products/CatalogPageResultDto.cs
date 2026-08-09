using Marketplace.Domain.Entities;

namespace Marketplace.BLL.DTOs.Products
{
    public class CatalogPageItemDto
    {
        public Product Product { get; set; } = null!;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class CatalogPageResultDto
    {
        public IReadOnlyList<CatalogPageItemDto> Items { get; set; } = Array.Empty<CatalogPageItemDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
