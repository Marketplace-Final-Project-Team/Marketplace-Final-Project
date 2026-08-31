using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
namespace Marketplace.BLL.Services
{
    public class WishlistCollectionService : IWishlistCollectionService
    {

        private readonly AppDbContext _context;
        private readonly IGenericRepository<Product> _productRepository;

        public WishlistCollectionService(AppDbContext context, IGenericRepository<Product> productRepository)
        {
            _context = context;
            _productRepository = productRepository;
        }

        public async Task<ServiceResult> AddToCollectionAsync(string userId, Guid collectionId, Guid productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return new ServiceResult{Succeeded=false, Message="Product does not exist"};
            var collection = await _context.WishlistCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId && !c.IsDeleted);
            if (collection == null)
                return new ServiceResult{Succeeded=false, Message="Collection does not exist"};
            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(c => c.WishlistCollectionId == collectionId && c.ProductId == productId && !c.IsDeleted);
            if (existing != null)
                return new ServiceResult{Succeeded=false, Message="Item already exists in collection"};

            // A product can only ever sit in one place — the base wishlist or a single
            // collection — never both, so this mirrors RemoveFromCollectionAsync's move
            // semantics instead of inserting a second row for the same product.
            var baseWishlistItem = await _context.WishlistItems
                .Include(i => i.Wishlist)
                .FirstOrDefaultAsync(i => i.ProductId == productId && !i.IsDeleted
                    && i.Wishlist != null && i.Wishlist.UserId == userId && !i.Wishlist.IsDeleted);

            if (baseWishlistItem != null)
            {
                baseWishlistItem.WishlistCollectionId = collectionId;
                baseWishlistItem.WishlistId = null;
            }
            else
            {
                var new_item = new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    WishlistCollectionId = collectionId,
                    WishlistId = null,
                    AddedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _context.WishlistItems.AddAsync(new_item);
            }

            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Item added to collection successfully"
            };
        }

        public async Task<ServiceResult> CreateCollectionAsync(string userId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new ServiceResult{Succeeded=false, Message="Name cannot be null or whitespace"};
            var new_collection = new WishlistCollection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _context.WishlistCollections.AddAsync(new_collection);
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Collection created successfully",
            };
            
        }

        public async Task<ServiceResult> DeleteCollectionAsync(string userId, Guid collectionId)
        {
            var collection = await _context.WishlistCollections
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId && !c.IsDeleted);

            if (collection == null)
                return new ServiceResult{Succeeded=false, Message="Collection does not exist"};
            collection.IsDeleted = true;
            foreach (var item in collection.Items)
            {
                item.IsDeleted = true;
            }
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Collection deleted successfully"
            };
        }

        public async Task<WishlistCollectionDetailDto?> GetCollectionAsync(string userId, Guid collectionId)
        {
            var collection = await _context.WishlistCollections
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId && !c.IsDeleted);
            if (collection == null) {
                return null;
            }
            return new WishlistCollectionDetailDto
            {
                Id = collection.Id,
                Name = collection.Name,
                CreatedAt = collection.CreatedAt,
                Items = collection.Items
                    .Where(i => !i.IsDeleted)
                    .Select(i => new WishlistItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Price = i.Product.Price,
                        MainImageUrl = i.Product.MainImageUrl
                    })
                    .ToList()
            };


        }

        public async Task<IEnumerable<WishlistCollectionDto>> GetCollectionsAsync(string userId)
        {
            var collections = await _context.WishlistCollections
                .Include(c=> c.Items)
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Select(c => new WishlistCollectionDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ItemCount = c.Items.Count(i => !i.IsDeleted),
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
            return collections;
        }

        public async Task<ServiceResult> RemoveFromCollectionAsync(string userId, Guid collectionId, Guid productId)
        {
            var item= await _context.WishlistItems
                .Include(i => i.WishlistCollection)
                .FirstOrDefaultAsync(i => i.WishlistCollectionId == collectionId && i.ProductId == productId && i.WishlistCollection.UserId == userId && !i.IsDeleted );

            if (item == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Item not found in the specified collection"
                };
            }
            var wishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted);
            if (wishlist == null)
            {
                wishlist = new Wishlist
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _context.Wishlists.AddAsync(wishlist);
                await _context.SaveChangesAsync();
            }

            item.WishlistId = wishlist.Id;
            item.WishlistCollectionId = null;
            item.IsDeleted = false;
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Item removed from collection successfully"
            };
        }

        public async Task<ServiceResult> UpdateCollectionAsync(string userId, Guid collectionId, string name)
        {
            var collection = await _context.WishlistCollections
                .Include(c=> c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId && !c.IsDeleted);

            if (collection == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Collection not found"
                };
            }
            collection.Name = name.Trim();
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Collection updated successfully"
            };
        }
    }
}
