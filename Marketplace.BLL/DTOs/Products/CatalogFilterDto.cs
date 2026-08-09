namespace Marketplace.BLL.DTOs.Products
{
    public class CatalogFilterDto
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public int? MinRating { get; set; }
        public CatalogSortOption Sort { get; set; } = CatalogSortOption.Newest;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
