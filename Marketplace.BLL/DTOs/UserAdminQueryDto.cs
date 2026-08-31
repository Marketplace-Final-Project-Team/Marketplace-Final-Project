using Marketplace.BLL.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class UserAdminQueryDto
    {
        public string? Search { get; set; }
        public UserStatus? Status { get; set; }
        public bool? SortDescending { get; set; }
        public PaginationParams Pagination { get; set; } = new PaginationParams();
    }
}
