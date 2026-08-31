using Marketplace.Domain.Entities;

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
                .Where(d => d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }

        // The business-wide discount currently active for this product's seller, if any.
        public static Discount? GetActiveBusinessDiscount(this Product product)
        {
            var now = DateTime.UtcNow;
            return product.Business?.Discounts?
                .Where(d => d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }

        // A business-wide sale is dominant over a product's own sale rather than stacking
        // with it — setting one is meant to replace the other (same rule as
        // OrderService.CreateOrder), capped at 100% so a price can never go to zero or below.
        public static decimal GetEffectiveDiscountPercent(this Product product)
        {
            var percent = product.GetActiveBusinessDiscount()?.Percent
                ?? product.GetActiveProductDiscount()?.Percent
                ?? 0;

            return percent > 100 ? 100 : percent;
        }

        public static decimal GetDiscountedPrice(this Product product)
        {
            var percent = product.GetEffectiveDiscountPercent();
            if (percent <= 0) return product.Price;

            return Math.Round(product.Price * (100 - percent) / 100m, 2);
        }
    }
}
