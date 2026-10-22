using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Services
{
    public class ReviewService : IReviewService
    {
        private const int MaxReviewBodyLength = 300;

        private readonly IGenericRepository<Review> _reviewRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IGenericRepository<ReviewAppeal> _reviewAppealRepo;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        public ReviewService(
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<Product> productRepo,
            IGenericRepository<ReviewAppeal> reviewAppealRepo,
            UserManager<User> userManager,
            AppDbContext context)
        {
            _reviewRepo = reviewRepo;
            _productRepo = productRepo;
            _reviewAppealRepo = reviewAppealRepo;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IReadOnlyList<ReviewDto>> GetReviewsByProductId(Guid productId)
        {
            var reviews = (await _reviewRepo.FindAsync(r => r.ProductId == productId && r.Status == ReviewStatus.Approved && !r.IsDeleted)).Select(r => new ReviewDto
            {
                Id = r.Id,
                Body = r.Body,
                Rating = r.Rating,
                UserId = r.UserId,
                UserName = r.User?.UserName ?? string.Empty,
                AvatarUrl = r.User?.AvatarUrl ?? string.Empty,
                PublicationDate = r.CreatedAt
            }).OrderByDescending(r => r.PublicationDate).ToList();

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

            var validationError = ValidateReviewContent(dto.Body, dto.Rating);
            if (validationError != null)
            {
                return new ServiceResult { Succeeded = false, Message = validationError };
            }

            var review = new Review
            {
                ProductId = dto.ProductId,
                UserId = dto.UserId,
                Body = dto.Body.Trim(),
                Rating = dto.Rating,
                Status = ReviewStatus.Approved
            };

            await _reviewRepo.AddAsync(review);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Відгук створено успішно" };
        }

        public async Task<ServiceResult> UpdateReview(ReviewUpdateDto dto)
        {
            var review = await _reviewRepo.GetByIdAsync(dto.ReviewId);
            if (review == null || review.IsDeleted)
            {
                return new ServiceResult { Succeeded = false, Message = "Відгук не знайдено" };
            }

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не було знайдено" };
            }

            if (review.UserId != user.Id)
            {
                return new ServiceResult { Succeeded = false, Message = "У вас немає прав для оновлення цього відгуку" };
            }

            var validationError = ValidateReviewContent(dto.Body, dto.Rating);
            if (validationError != null)
            {
                return new ServiceResult { Succeeded = false, Message = validationError };
            }
            if (review.Status == ReviewStatus.Rejected)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Відхилений відгук можна змінити тільки через апеляцію"
                };
            }

            review.Body = dto.Body.Trim();
            review.Rating = dto.Rating;

            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Відгук оновлено успішно" };
        }

        public async Task<ServiceResult> DeleteReview(Guid reviewId, string userId)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);
            if (review == null || review.IsDeleted)
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

            if (await _context.ReviewAppeals.AnyAsync(a =>
                    a.ReviewId == reviewId && a.Status == ReviewAppealStatus.Pending))
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Відгук не можна видалити, поки апеляція очікує розгляду"
                };
            }

            review.IsDeleted = true;
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Відгук видалено успішно" };
        }

        public async Task<ReviewUpdateDto?> GetReviewUpdateDtoAsync(Guid reviewId, string userId)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);

            if (review == null || review.IsDeleted || review.Status == ReviewStatus.Rejected || review.UserId != userId)
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

        public async Task<ServiceResult> CreateReviewAppeal(ReviewAppealCreateDto dto)
        {
            var review = await _reviewRepo.GetByIdAsync(dto.ReviewId);

            if (review == null || review.IsDeleted)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Відгук не знайдено"
                };
            }

            if (review.UserId != dto.UserId)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "У вас немає прав для подачі апеляції",
                    EntityId = review.ProductId
                };
            }

            if (review.Status != ReviewStatus.Rejected)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Апеляцію можна подати тільки для відхиленого відгуку",
                    EntityId = review.ProductId
                };
            }

            var validationError = ValidateReviewContent(dto.Body, dto.Rating);
            if (validationError != null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = validationError,
                    EntityId = review.ProductId
                };
            }

            var existingAppeal = await _reviewAppealRepo.FindAsync(a =>
                a.ReviewId == dto.ReviewId &&
                a.Status == ReviewAppealStatus.Pending);

            if (existingAppeal.Any())
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "У вас вже є апеляція, яка очікує перевірки",
                    EntityId = review.ProductId
                };
            }

            var appeal = new ReviewAppeal
            {
                ReviewId = dto.ReviewId,
                UserId = dto.UserId,
                OriginalBody = review.Body,
                OriginalRating = review.Rating,
                OriginalModerationReason = review.ModerationReason,
                Body = dto.Body.Trim(),
                Rating = dto.Rating,
                Status = ReviewAppealStatus.Pending
            };

            await _reviewAppealRepo.AddAsync(appeal);
            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Succeeded = true,
                Message = "Апеляцію успішно подано",
                EntityId = review.ProductId
            };
        }

        private static string? ValidateReviewContent(string? body, float rating)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return "Текст відгуку є обов'язковим";
            }

            if (body.Trim().Length > MaxReviewBodyLength)
            {
                return $"Текст відгуку не може перевищувати {MaxReviewBodyLength} символів";
            }

            if (!float.IsFinite(rating) || rating < 1 || rating > 5)
            {
                return "Оцінка повинна бути в діапазоні від 1 до 5";
            }

            return null;
        }

        public async Task<int> GetReviewCountByBusinessAsync(Guid businessId) 
        {
            var count = await _context.Reviews
                .Where(r => r.Product.BusinessId == businessId && r.Status == ReviewStatus.Approved && !r.IsDeleted)
                .CountAsync();

            return count;
        }

        public async Task<float> GetAvgRatingByBusinessAsync(Guid businessId)
        {
            var avg = await _context.Reviews
                .Where(r => r.Product.BusinessId == businessId && r.Status == ReviewStatus.Approved && !r.IsDeleted)
                .AverageAsync(r => (float?)r.Rating);

            return avg.HasValue ? (float)Math.Round(avg.Value, 1) : 0f;
        }
    }
}
