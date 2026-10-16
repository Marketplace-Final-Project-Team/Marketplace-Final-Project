using Marketplace.BLL.DTOs.Products;
using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class CatalogueFilterViewModel
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public int? MinRating { get; set; }
        public CatalogueSortOption Sort { get; set; } = CatalogueSortOption.Newest;
        public int Page { get; set; } = 1;

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(Search)
            || CategoryId.HasValue
            || MinPrice.HasValue
            || MaxPrice.HasValue
            || InStockOnly
            || MinRating.HasValue
            || Sort != CatalogueSortOption.Newest;
    }
}
