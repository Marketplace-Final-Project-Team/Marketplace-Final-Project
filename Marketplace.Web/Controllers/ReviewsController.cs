using Marketplace.Web.Infrastructure;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly UserManager<User> _userManager;

        public ReviewsController(IReviewService reviewService, UserManager<User> userManager)
        {
            _reviewService = reviewService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> CreateReview(Guid productId)
        {
            var model = new ReviewCreateViewModel
            {
                ProductId = productId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview(ReviewCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            
            var result = await _reviewService.CreateReview(new ReviewCreateDto
            {
                ProductId = model.ProductId,
                UserId = userId!,
                Body = model.Body,
                Rating = model.Rating
            });

            if (!result.Succeeded) 
            {
                ModelState.AddModelError("", this.L(result.Message));
                return View(model);
            }


            return RedirectToAction("Details", "Catalog", new { id = model.ProductId });
        }

        [HttpGet]
        public async Task<IActionResult> UpdateReview(Guid productId, Guid reviewId)
        {
            var userId = _userManager.GetUserId(User);
            var review = await _reviewService.GetReviewUpdateDtoAsync(reviewId, userId!);

            if(review == null) 
            {
                return NotFound();
            }

            return View(new ReviewUpdateViewModel 
            {
                ReviewId = reviewId,
                ProductId = productId,
                Body = review.Body,
                Rating = review.Rating
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReview(ReviewUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var result = await _reviewService.UpdateReview(new ReviewUpdateDto
            {
                ReviewId = model.ReviewId,
                UserId = userId!,
                Body = model.Body,
                Rating = model.Rating
            });
            
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", this.L(result.Message));
                return View(model);
            }
            
            return RedirectToAction("Details", "Catalog", new { id = model.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(Guid productId, Guid reviewId)
        {
            var userId = _userManager.GetUserId(User);

            var result = await _reviewService.DeleteReview(reviewId, userId!);
            if (!result.Succeeded)
            {
                TempData["Error"] = this.L(result.Message);
            }

            return RedirectToAction("Details", "Catalog", new { id = productId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReviewAppeal(ReviewAppealCreateDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            dto.UserId = userId;
            var result = await _reviewService.CreateReviewAppeal(dto);
            if (!result.Succeeded)
            {
                TempData["Error"] = this.L(result.Message);
            }
            else
            {
                TempData["Success"] = this.L(result.Message);
            }

            return result.EntityId is Guid productId
                ? RedirectToAction("Details", "Catalog", new { id = productId })
                : RedirectToAction("Index", "Home");
        }
    }
}
