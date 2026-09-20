using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class RecommendationController : Controller
    {
        private readonly IRecommendationService _recomendationService;
        private readonly UserManager<User> _userManager;
        public RecommendationController(IRecommendationService recomendationService, UserManager<User> userManager)
        {
            _recomendationService = recomendationService;
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetRecomendation(int count = 5)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var recommendations = await _recomendationService.GetRecomendationAsync(user.Id, count);
            return Ok(recommendations);
        }
    }
}
