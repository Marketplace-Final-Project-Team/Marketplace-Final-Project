namespace Marketplace.Web.Models.Products
{
    public class ProductsIndexViewModel
    {
        public List<ProductListItemViewModel> Items { get; set; } = new();
        public string? Search { get; set; }
        public string Filter { get; set; } = "all";

        public int AllCount { get; set; }
        public int ActiveCount { get; set; }
        public int PendingCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int HiddenCount { get; set; }
    }
}
