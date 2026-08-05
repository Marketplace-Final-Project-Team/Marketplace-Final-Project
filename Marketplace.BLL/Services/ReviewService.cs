using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Net.WebSockets;

namespace Marketplace.BLL.Services
{
    public class ReviewService: IReviewService
    {
        private readonly IGenericRepository<Review> _reviewRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        public ReviewService(IGenericRepository<Review> reviewRepo, IGenericRepository<Product> productRepo, UserManager<User> userManager, AppDbContext context)
        {
            _reviewRepo = reviewRepo;
            _productRepo = productRepo;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IReadOnlyList<ReviewDto>> GetReviewsByProductId(Guid productId) 
        {
            var reviews = (await _reviewRepo.FindAsync(r => r.ProductId == productId)).Select(r => new ReviewDto
            {
                Body = r.Body,
                Rating = r.Rating,
                UserName = r.User.UserName!,
                AvatarUrl = r.User.AvatarUrl,
                PublicationDate = r.CreatedAt
            }).ToList();

            return reviews;
        }

        public async Task<ServiceResult> CreateReview(ReviewCreateDto dto) 
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не було знайдено" };
            }
            
            if (!await _productRepo.ExistsAsync(p => p.Id == dto.ProductId))
            {
                return new ServiceResult { Succeeded = false, Message = "Продукт не знайдено" };
            } 

            if(dto.Rating < 0 || dto.Rating > 5)
            {
                return new ServiceResult { Succeeded = false, Message = "Некоректна оцінка" };
            }

            var review = new Review
            {
                ProductId = dto.ProductId,
                UserId = dto.UserId,
                Body = dto.Body,
                Rating = dto.Rating
            };
            
            await _reviewRepo.AddAsync(review);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Відгук створено успішно" };
        }   

        public async Task<ServiceResult> UpdateReview(ReviewUpdateDto dto)
        {
            var review = await _reviewRepo.GetByIdAsync(dto.ReviewId);
            if (review == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Відгук не знайдено" };
            }

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не було знайдено" };
            }

            if(review.UserId != user.Id)
            {
                return new ServiceResult { Succeeded = false, Message = "У вас немає прав для оновлення цього відгуку" };
            }

            if (dto.Rating < 0 || dto.Rating > 5)
            {
                return new ServiceResult { Succeeded = false, Message = "Некоректна оцінка" };
            }

            review.Body = dto.Body;
            review.Rating = dto.Rating;

            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Відгук оновлено успішно" };
        }

        public async Task<ServiceResult> DeleteReview(Guid reviewId, string userId) 
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);
            if (review == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Відгук не знайдено" };
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не було знайдено" };
            }
            
            if (review.UserId != user.Id)
            {
                return new ServiceResult { Succeeded = false, Message = "У вас немає прав для видалення цього відгуку" };
            }

            review.IsDeleted = true;
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Відгук видалено успішно" };
        }

        public async Task<ReviewUpdateDto?> GetReviewUpdateDtoAsync(Guid reviewId, string userId) 
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);

            if (review == null || review.UserId != userId) 
            {
                return null;
            }

            return new ReviewUpdateDto
            {
                ReviewId = review.Id,
                UserId = review.UserId,
                Body = review.Body,
                Rating = review.Rating
            };
        }
    }
}
