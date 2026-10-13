using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Pricing;
namespace Marketplace.Web.Models
{
    public static class DiscountExtensions
    {
        // The discount the seller set on this specific product — used by the product Edit
        // page, whose "set/cancel discount" form only ever acts on a product-level Discount
        // row, never the business-wide one.
        public static Discount? GetActiveProductDiscount(this Product product)
        {
            var now = DateTime.UtcNow;
            return product.Discounts?
                .Where(d => !d.IsDeleted && d.ProductId == product.Id && d.DiscountType == DiscountType.Product && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.Percent)
                .ThenByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }

        // The business-wide discount currently active for this product's seller, if any.
        public static Discount? GetActiveBusinessDiscount(this Product product)
        {
            var now = DateTime.UtcNow;
            return product.Business?.Discounts?
                .Where(d => !d.IsDeleted && d.DiscountType == DiscountType.Business && d.BusinessId == product.BusinessId && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.Percent)
                .ThenByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }

        public static Discount? GetActiveCategoryDiscount(this Product product)
        {
            var now = DateTime.UtcNow;
            return product.Category?.Discounts?
                .Where(d => !d.IsDeleted && d.DiscountType == DiscountType.Category && d.BusinessId == product.BusinessId && d.CategoryId == product.CategoryId && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.Percent)
                .ThenByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }
        public static UserDiscount? GetActiveUserDiscount(this Product product, string? userId)
        {
            var now = DateTime.UtcNow;
            if (string.IsNullOrEmpty(userId)) return null;
            return product.Business?.UserDiscounts?
                .Where(d => !d.IsDeleted && d.UserId == userId && d.BusinessId == product.BusinessId && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.Percent)
                .ThenByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }

        public static decimal GetEffectiveDiscountPercent(this Product product, string? userId = null) =>
            DiscountCalculator.GetEffectiveDiscountPercent(product, userId);

        public static decimal GetDiscountedPrice(this Product product, string? userId = null) =>
            DiscountCalculator.CalculatePrice(product.Price, product.GetEffectiveDiscountPercent(userId));
    }
}
