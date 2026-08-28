using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class UserAdminFilterViewModel
    {
        public string? Search { get; set; }
        public UserStatus? Status { get; set; }
        public bool? SortDescending { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
