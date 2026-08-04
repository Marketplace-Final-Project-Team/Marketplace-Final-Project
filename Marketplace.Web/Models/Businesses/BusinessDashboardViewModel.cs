using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models.Businesses;

public class BusinessDashboardViewModel
{
    public IReadOnlyList<BusinessDashboardItemViewModel> Businesses { get; init; }
        = Array.Empty<BusinessDashboardItemViewModel>();
    public bool CanCreateBusiness { get; init; }
}

public class BusinessDashboardItemViewModel
{
    public Guid Id { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string LogoUrl { get; init; } = string.Empty;
    public string BannerUrl { get; init; } = string.Empty;
    public BusinessStatus Status { get; init; }
    public bool IsOwner { get; init; }
    public int ProductCount { get; init; }
    public int ActiveProductCount { get; init; }
    public int PendingProductCount { get; init; }
    public int OutOfStockProductCount { get; init; }
    public int OrderCount { get; init; }
    public int AwaitingOrderCount { get; init; }
    public int SoldItemCount { get; init; }
    public decimal Revenue { get; init; }
}
