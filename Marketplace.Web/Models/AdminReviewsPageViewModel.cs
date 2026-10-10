using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class AdminReviewFilterViewModel
    {
        public string? Search { get; set; }
        public string Tab { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminReviewsPageViewModel
    {
        public AdminReviewFilterViewModel Filter { get; set; } = new();
        public PagedResult<ReviewAdminDto> Result { get; set; } = new();
        public IReadOnlyList<ReviewAppealAdminDto> Appeals { get; set; } = new List<ReviewAppealAdminDto>();
        public int TotalCount { get; set; }
        public int RejectedCount { get; set; }
    }
}
