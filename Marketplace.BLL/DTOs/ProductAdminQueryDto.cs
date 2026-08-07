using Marketplace.BLL.Common;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class ProductAdminQueryDto
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public ProductSortOption SortOption { get; set; } = ProductSortOption.PopularityDesc;
        public PaginationParams Pagination { get; set; } = new PaginationParams();
    }
}
