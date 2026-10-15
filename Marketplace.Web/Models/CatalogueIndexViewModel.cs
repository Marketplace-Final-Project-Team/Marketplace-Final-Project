using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Models;

public class CatalogueIndexViewModel
{
    public IReadOnlyList<CatalogueProductViewModel> Products { get; set; } = new List<CatalogueProductViewModel>();
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public IEnumerable<SelectListItem>? Categories { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}