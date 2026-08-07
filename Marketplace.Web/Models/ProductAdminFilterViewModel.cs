using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class ProductAdminFilterViewModel
    {
        public Guid? CategoryId { get; set; }
        public string? Search { get; set; }
        public ProductSortOption SortOption { get; set; } = ProductSortOption.CreatedAtDesc;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
