using Marketplace.BLL.DTOs.Products;
using Marketplace.Domain.Entities;

namespace Marketplace.Web.Models
{
    public static class ProductMappingExtensions
    {
        public static CatalogProductViewModel ToCatalogProductViewModel(this Product product)
        {
            var activeDiscount = product.GetActiveDiscount();

            return new CatalogProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = activeDiscount != null ? product.GetDiscountedPrice() : product.Price,
                Quantity = product.Quantity,
                MainImageUrl = product.MainImageUrl,
                CategoryName = product.Category?.Name ?? "Без категорії",
                StoreName = product.Business?.StoreName ?? "Marketplace",
                StoreLogoUrl = product.Business?.LogoUrl ?? string.Empty,
                StoreDescription = product.Business?.Description ?? string.Empty,
                AdditionalImageUrls = product.Images.Select(image => image.ImageUrl).ToList(),
                OriginalPrice = activeDiscount != null ? product.Price : null,
                DiscountPercent = activeDiscount?.Percent
            };
        }

        public static CatalogProductViewModel ToCatalogProductViewModel(this CatalogProductDto dto)
        {
            return new CatalogProductViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Price = dto.HasActiveDiscount && dto.DiscountedPrice.HasValue
                    ? dto.DiscountedPrice.Value
                    : dto.Price,
                MainImageUrl = dto.MainImageUrl,
                CategoryName = dto.CategoryName ?? "Без категорії",
                OriginalPrice = dto.HasActiveDiscount ? dto.Price : null,
                DiscountPercent = dto.DiscountPercentage.HasValue
                    ? (int)dto.DiscountPercentage.Value
                    : null
            };
        }
    }
}