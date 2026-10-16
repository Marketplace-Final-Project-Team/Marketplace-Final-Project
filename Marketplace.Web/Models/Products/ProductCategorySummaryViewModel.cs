namespace Marketplace.Web.Models.Products
{
    public class ProductCategorySummaryViewModel
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int ActiveProductCount { get; set; }
    }
}
