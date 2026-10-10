using Marketplace.BLL.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class ReviewAdminQueryDto
    {
        public string? Search { get; set; }
        public ReviewStatus? Status { get; set; }
        public PaginationParams Pagination { get; set; } = new PaginationParams();
    }
}
