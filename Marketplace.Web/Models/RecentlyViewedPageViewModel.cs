namespace Marketplace.Web.Models;

public class RecentlyViewedPageViewModel
{
    public List<CatalogRelatedProductViewModel> Items { get; set; } = new();
    public bool ConsentAccepted { get; set; }
}
