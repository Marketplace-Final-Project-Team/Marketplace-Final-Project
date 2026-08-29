namespace Marketplace.Web.Models;

public class BusinessLimitsViewModel
{
    public int MaxProductsPerBusiness { get; set; } = 200;
    public int MaxDiscountPercent { get; set; } = 90;
    public int MaxPromoCodePercent { get; set; } = 90;
    public int MaxActivePromoCodesPerBusiness { get; set; } = 20;
    public int PendingOrderExpiryHours { get; set; } = 48;
    public int LowStockThreshold { get; set; } = 5;
}
