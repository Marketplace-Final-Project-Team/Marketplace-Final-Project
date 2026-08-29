using Marketplace.Domain.Entities;

namespace Marketplace.Web.Models
{
    public static class DiscountExtensions
    {

        public static Discount? GetActiveDiscount(this Product product)
        {
            var now = DateTime.UtcNow;

            
            var productDiscount = product.Discounts?
                .Where(d => d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();

            
            var businessDiscount = product.Business?.Discounts?
                .Where(d => d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();

           
            if (productDiscount != null && businessDiscount != null)
            {
                return productDiscount.CreatedAt > businessDiscount.CreatedAt
                    ? productDiscount
                    : businessDiscount;
            }

            return productDiscount ?? businessDiscount;
        }

        public static decimal GetDiscountedPrice(this Product product)
        {
            var discount = product.GetActiveDiscount();
            if (discount == null) return product.Price;

            return Math.Round(product.Price * (100 - discount.Percent) / 100m, 2);
        }
    }
}