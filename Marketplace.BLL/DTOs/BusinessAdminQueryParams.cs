using Marketplace.BLL.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class BusinessAdminQueryParams
    {
        public string? Search { get; set; }
        public string? SearchEmail { get; set; }
        public BusinessStatus? Status { get; set; }
        public decimal? MinCommission { get; set; }
        public decimal? MaxCommission { get; set; }
        public bool? SortDescending { get; set; }
        public PaginationParams Pagination { get; set; } = new PaginationParams();
    }
}
