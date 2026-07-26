using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Services;
using Marketplace.Domain.Entities;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class BusinessController : Controller
    {
        private readonly IBusinessService _businessService;
        private readonly UserManager<User> _userManager;

        public BusinessController(IBusinessService businessService, UserManager<User> userManager)
        {
            _businessService = businessService;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BusinessCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var result = await _businessService.CreateBusiness(new BusinessCreateDto
            {
                StoreName = model.StoreName,
                Description = model.Description,
                UserId = userId!,
                Logo = model.Logo,
                Banner = model.Banner
            });

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
