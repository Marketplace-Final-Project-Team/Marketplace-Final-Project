using Marketplace.BLL.Common;

namespace Marketplace.Web.Models
{
    public class AdminUsersPageViewModel
    {
        public PagedResult<UserItemAdminViewModel> Result { get; set; } = new();
        public UserAdminFilterViewModel Filter { get; set; } = new();
    }
}
