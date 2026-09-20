namespace Marketplace.BLL.DTOs.Products
{
    public class ProductImportRowError
    {
        public int RowNumber { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    public class ProductImportResultDto
    {
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public List<ProductImportRowError> RowErrors { get; set; } = new();
    }
}
