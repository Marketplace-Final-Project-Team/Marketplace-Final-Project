using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IGenericRepository<Wishlist> _wishlistRepository;
        private readonly IGenericRepository<WishlistItem> _wishlistItemRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly AppDbContext _context;
        private readonly IProductAnalyticsService _productAnalyticsService;

        public WishlistService(
            IGenericRepository<Wishlist> wishlistRepository,
            IGenericRepository<WishlistItem> wishlistItemRepository,
            IGenericRepository<Product> productRepository,
            AppDbContext context,
            IProductAnalyticsService productAnalyticsService)
        {
            _wishlistRepository = wishlistRepository;
            _wishlistItemRepository = wishlistItemRepository;
            _productRepository = productRepository;
            _context = context;
            _productAnalyticsService = productAnalyticsService;
        }

        public async Task<WishlistDto> GetWishlistAsync(string userId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .ThenInclude(wi => wi.Product)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wishlist == null)
            {
                wishlist = new Wishlist
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _wishlistRepository.AddAsync(wishlist);
                await _context.SaveChangesAsync();
            }

            return new WishlistDto
            {
                Id = wishlist.Id,
                Items = wishlist.WishlistItems?.Select(item => new WishlistItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? "Невідомий товар",
                    Price = item.Product?.Price ?? 0,
                    MainImageUrl = item.Product?.MainImageUrl ?? string.Empty,


                }).ToList() ?? new List<WishlistItemDto>(),

            };
        }
        public async Task<ServiceResult> AddToWishlistAsync(string userId, Guid productId)
        {
            var product = await _productRepository.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено." };
            }
            var wishlist = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wishlist == null)
            {
                wishlist = new Wishlist
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _wishlistRepository.AddAsync(wishlist);
                await _context.SaveChangesAsync();
            }


            var existingItem = await _wishlistItemRepository
                .FirstOrDefaultAsync(wi => wi.WishlistId == wishlist.Id && wi.ProductId == productId);

            if (existingItem != null)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар уже в списку бажаного." };
            }


            var wishlistItem = new WishlistItem
            {
                Id = Guid.NewGuid(),
                WishlistId = wishlist.Id,
                ProductId = productId,
                AddedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _wishlistItemRepository.AddAsync(wishlistItem);
            await _context.SaveChangesAsync();

            await _productAnalyticsService.RecordEventAsync(productId, product.BusinessId, ProductEventType.AddToWishlist, userId);

            return new ServiceResult { Succeeded = true, Message = "Товар додано до списку бажаного." };

        }
        public async Task<ServiceResult> RemoveFromWishlistAsync(string userId, Guid productId)
        {
            var item = await _context.WishlistItems
            .Include(wi => wi.Wishlist)
            .FirstOrDefaultAsync(wi => wi.Wishlist.UserId == userId && wi.ProductId == productId);
            if (item == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено в списку бажаного." };
            }
            _wishlistItemRepository.Remove(item);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Товар видалено з списку бажаного." };

        }

        
    }
}
