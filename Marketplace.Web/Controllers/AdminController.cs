using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Enums;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService) 
        {
            _adminService = adminService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Users() 
        {
            var result = await _adminService.GetAllUsers();
            return View(result.Select(u => new UserItemAdminViewModel 
            {
                Id = u.Id,
                Email = u.Email,
                Status = u.Status,
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Products() 
        {
            var result = await _adminService.GetAllProducts();
            return View(result.Select(p => new ProductItemAdminViewModel 
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                Status = p.Status,
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Businesses() 
        {
            var result = await _adminService.GetAllBusinesses();
            return View(result.Select(b => new BusinessItemAdminViewModel 
            {
                Id = b.Id,
                Name = b.Name,
                LogoUrl = b.LogoUrl,
                Owner = b.Owner,
                Status = b.Status,
            }));
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserStatus(string userId, UserStatus status) 
        {
            var result = await _adminService.ChangeUserStatus(userId, status);
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeProductStatus(Guid productId, ProductStatus status)
        {
            var result = await _adminService.ChangeProductStatus(productId, status);
            return RedirectToAction("Products");
        }


        [HttpPost]
        public async Task<IActionResult> ChangeBusinessStatus(Guid businessId, BusinessStatus status)
        {
            var result = await _adminService.ChangeBusinessStatus(businessId, status);
            return RedirectToAction("Businesses");
        }
    }
}
