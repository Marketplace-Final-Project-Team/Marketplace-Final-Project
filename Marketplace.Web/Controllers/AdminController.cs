using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Enums;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Crmf;

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

        public async Task<IActionResult> Index()
        {
            var users = await _adminService.GetAllUsers();
            var products = await _adminService.GetAllProducts();
            var businesses = await _adminService.GetAllBusinesses();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                BannedUsers = users.Count(u => u.Status == UserStatus.Banned),
                TotalProducts = products.Count,
                PendingProducts = products.Count(p => p.Status == ProductStatus.Pending),
                BannedProducts = products.Count(p => p.Status == ProductStatus.Banned),
                TotalBusinesses = businesses.Count,
                PendingBusinesses = businesses.Count(b => b.Status == BusinessStatus.Pending),
            };

            return View(model);
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
