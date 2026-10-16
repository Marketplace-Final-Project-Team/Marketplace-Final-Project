using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Pricing;

public static class DiscountCalculator
{
    public static decimal GetEffectiveDiscountPercent(Product product, string? userId = null, DateTime? at = null)
    {
        var discounts = product.Discounts
            .Concat(product.Business?.Discounts ?? Enumerable.Empty<Discount>())
            .Concat(product.Category?.Discounts ?? Enumerable.Empty<Discount>());

        return GetEffectiveDiscountPercent(product.Id, product.BusinessId, product.CategoryId,
            discounts, product.Business?.UserDiscounts ?? Enumerable.Empty<UserDiscount>(), userId, at);
    }

    public static decimal GetEffectiveDiscountPercent(
        Guid productId, Guid businessId, Guid categoryId,
        IEnumerable<Discount> discounts, IEnumerable<UserDiscount> userDiscounts,
        string? userId = null, DateTime? at = null)
    {
        var now = at ?? DateTime.UtcNow;
        var publicPercent = discounts
            .Where(d => !d.IsDeleted && d.StartDate <= now && d.EndDate >= now &&
                (d.DiscountType == DiscountType.Product && d.ProductId == productId ||
                 d.DiscountType == DiscountType.Business && d.BusinessId == businessId ||
                 d.DiscountType == DiscountType.Category && d.BusinessId == businessId && d.CategoryId == categoryId))
            .Select(d => d.Percent)
            .DefaultIfEmpty(0m)
            .Max();

        var personalPercent = string.IsNullOrEmpty(userId) ? 0m : userDiscounts
            .Where(d => !d.IsDeleted && d.UserId == userId && d.BusinessId == businessId &&
                d.StartDate <= now && d.EndDate >= now)
            .Select(d => d.Percent)
            .DefaultIfEmpty(0m)
            .Max();

        return Math.Clamp(Math.Max(publicPercent, personalPercent), 0m, 100m);
    }

    public static decimal CalculatePrice(decimal price, decimal discountPercent) =>
        Math.Round(price * (1m - Math.Clamp(discountPercent, 0m, 100m) / 100m),
            2, MidpointRounding.AwayFromZero);
}
