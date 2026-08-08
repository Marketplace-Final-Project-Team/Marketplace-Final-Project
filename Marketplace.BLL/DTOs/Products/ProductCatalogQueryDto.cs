namespace Marketplace.BLL.DTOs.Products
{
    public class ProductCatalogQueryDto
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}