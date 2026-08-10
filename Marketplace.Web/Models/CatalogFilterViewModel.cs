using Marketplace.BLL.DTOs.Products;

namespace Marketplace.Web.Models
{
    public class CatalogFilterViewModel
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public int? MinRating { get; set; }
        public CatalogSortOption Sort { get; set; } = CatalogSortOption.Newest;
        public int Page { get; set; } = 1;

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(Search)
            || CategoryId.HasValue
            || MinPrice.HasValue
            || MaxPrice.HasValue
            || InStockOnly
            || MinRating.HasValue
            || Sort != CatalogSortOption.Newest;
    }
}
