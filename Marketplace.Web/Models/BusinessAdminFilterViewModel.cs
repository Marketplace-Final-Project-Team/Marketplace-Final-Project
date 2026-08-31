using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class BusinessAdminFilterViewModel
    {
        public string? Search { get; set; }
        public BusinessStatus? Status { get; set; }
        public BusinessSortOption SortOption { get; set; } = BusinessSortOption.CreatedAtDesc;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
