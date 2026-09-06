using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
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
                .Where(d => d.ProductId == product.Id && d.DiscountType == DiscountType.Product && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }

        // The business-wide discount currently active for this product's seller, if any.
        public static Discount? GetActiveBusinessDiscount(this Product product)
        {
            var now = DateTime.UtcNow;
            return product.Business?.Discounts?
                .Where(d => d.DiscountType == DiscountType.Business && d.BusinessId == product.BusinessId && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }

        public static Discount? GetActiveCategoryDiscount(this Product product)
        {
            var now = DateTime.UtcNow;
            return product.Category?.Discounts?
                .Where(d => d.DiscountType == DiscountType.Category && d.BusinessId == product.BusinessId && d.CategoryId == product.CategoryId && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }
        public static UserDiscount? GetActiveUserDiscount(this Product product, string userId)
        {
            var now = DateTime.UtcNow;
            return product.Business?.UserDiscounts?
                .Where(d => d.UserId == userId && d.BusinessId == product.BusinessId && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }





        // A business-wide sale is dominant over a product's own sale rather than stacking
        // with it — setting one is meant to replace the other (same rule as
        // OrderService.CreateOrder), capped at 100% so a price can never go to zero or below.
        public static decimal GetEffectiveDiscountPercent(this Product product,string userId)
        {
            decimal? discount = 0;
            var productobj = product.GetActiveProductDiscount();
            var categoryobj = product.GetActiveCategoryDiscount();
            var businessobj = product.GetActiveBusinessDiscount();
            var userobj = product.GetActiveUserDiscount(userId);
            if (userobj != null)
            {
                var alldiscounts = new List<decimal>();
                if (productobj != null)
                {
                    alldiscounts.Add(productobj.Percent);
                }
                if (categoryobj != null)
                {
                    alldiscounts.Add(categoryobj.Percent);
                }
                if (userobj != null)
                {
                    alldiscounts.Add(userobj.Percent);
                }
                if (businessobj != null)
                {
                    alldiscounts.Add(businessobj.Percent);
                }
                discount = alldiscounts.Any() ? alldiscounts.Max() : 0;
            }
            else
            {
                if (productobj != null)
                {
                    discount = productobj.Percent;
                }
                else if (categoryobj != null)
                {
                    discount = categoryobj.Percent;
                }
                else if (businessobj != null)
                {
                    discount = businessobj.Percent;
                }
            }
            var percent = discount ?? 0;
            return percent > 90 ? 90 : percent;
        }

        public static decimal GetDiscountedPrice(this Product product, string userId)
        {
            var percent = product.GetEffectiveDiscountPercent(userId);
            if (percent <= 0) return product.Price;

            return Math.Round(product.Price * (100 - percent) / 100m, 2);
        }
    }
}
