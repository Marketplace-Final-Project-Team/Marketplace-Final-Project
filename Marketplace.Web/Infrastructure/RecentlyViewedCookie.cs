namespace Marketplace.Web.Infrastructure;

// Recently-viewed products are tracked with a plain cookie holding just product ids
// (no name/price/image cached in the cookie) so the list is always resolved against
// the database when rendered — a product that was deleted, banned, or sold out since
// the visit simply disappears instead of showing stale cached data.
public static class RecentlyViewedCookie
{
    public const string Name = "mp_recently_viewed";
    public const int MaxItems = 8;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public static List<Guid> Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new List<Guid>();
        }

        return rawValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => Guid.TryParse(segment, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }

    public static string Serialize(IEnumerable<Guid> ids) => string.Join(',', ids);

    public static async Task<List<Marketplace.Web.Models.CatalogRelatedProductViewModel>> LoadAsync(
        Marketplace.BLL.Interfaces.IProductService productService,
        IRequestCookieCollection cookies)
    {
        cookies.TryGetValue(Name, out var raw);

        var items = new List<Marketplace.Web.Models.CatalogRelatedProductViewModel>();
        foreach (var id in Parse(raw))
        {
            // Re-fetched on every request so a product deleted, banned, or sold out since
            // the visit just drops out instead of showing stale name/price/stock data.
            var product = await productService.GetCatalogueProductAsync(id);
            if (product is null)
            {
                continue;
            }

            items.Add(new Marketplace.Web.Models.CatalogRelatedProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryName = product.Category?.Name ?? "Без категорії",
                MainImageUrl = product.MainImageUrl,
                Price = product.Price,
                Quantity = product.Quantity
            });
        }

        return items;
    }
}
