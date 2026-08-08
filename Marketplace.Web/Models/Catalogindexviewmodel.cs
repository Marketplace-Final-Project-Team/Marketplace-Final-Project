using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Models;

public class CatalogIndexViewModel
{
    public IReadOnlyList<CatalogProductViewModel> Products { get; set; } = new List<CatalogProductViewModel>();
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public IEnumerable<SelectListItem>? Categories { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}