using Marketplace.BLL.Common;

namespace Marketplace.Web.Models
{
    public class AdminBusinessesPageViewModel
    {
        public PagedResult<BusinessItemAdminViewModel> Result { get; set; } = new();
        public BusinessAdminFilterViewModel Filter { get; set; } = new();
    }
}
