using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models;

public class BusinessLimitsViewModel
{
    [Range(1, 100000)]
    public int MaxProductsPerBusiness { get; set; } = 200;

    [Range(1, 100)]
    public int MaxDiscountPercent { get; set; } = 90;

    [Range(1, 100)]
    public int MaxPromoCodePercent { get; set; } = 90;

    [Range(1, 1000)]
    public int MaxActivePromoCodesPerBusiness { get; set; } = 20;

    [Range(1, 8760)]
    public int PendingOrderExpiryHours { get; set; } = 48;

    [Range(0, 100000)]
    public int LowStockThreshold { get; set; } = 5;
}
