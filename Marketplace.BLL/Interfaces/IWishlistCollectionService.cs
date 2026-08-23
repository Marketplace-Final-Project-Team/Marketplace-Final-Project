using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.Interfaces
{
    public interface IWishlistCollectionService
    {
        Task<IEnumerable<WishlistCollectionDto>> GetCollectionsAsync(string userId);

        // Отримати одну колекцію з товарами
        Task<WishlistCollectionDetailDto?> GetCollectionAsync(string userId, Guid collectionId);

        // Створити нову колекцію
        Task<ServiceResult> CreateCollectionAsync(string userId, string name);

        // Оновити назву/опис колекції
        Task<ServiceResult> UpdateCollectionAsync(string userId, Guid collectionId, string name);

        // Видалити колекцію (разом із товарами в ній)
        Task<ServiceResult> DeleteCollectionAsync(string userId, Guid collectionId);

        // Додати товар до колекції
        Task<ServiceResult> AddToCollectionAsync(string userId, Guid collectionId, Guid productId);

        // Видалити товар із колекції
        Task<ServiceResult> RemoveFromCollectionAsync(string userId, Guid collectionId, Guid productId);
    }
}
