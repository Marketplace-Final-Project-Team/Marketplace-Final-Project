using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs.Products
{
    public class CatalogueFilterDto
    {
        public Guid? CategoryId { get; set; }
        public string? Search { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public double? MinRating { get; set; }
        public CatalogueSortOption Sort { get; set; } = CatalogueSortOption.Newest;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
