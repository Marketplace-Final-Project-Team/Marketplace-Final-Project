using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Models
{
    public class CatalogPageViewModel
    {
        public CatalogFilterViewModel Filter { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public List<CatalogProductViewModel> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
