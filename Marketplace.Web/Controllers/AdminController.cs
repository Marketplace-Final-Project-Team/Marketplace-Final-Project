using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Enums;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Common;

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
            var users = _adminService.GetUsers();
            var products = await _adminService.GetProductsAsync();
            var businesses = await _adminService.GetBusinessesAsync();

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
        public async Task<IActionResult> Users([FromQuery] UserAdminFilterViewModel filter) 
        {
            var result = await _adminService.GetFilteredUsersAsync(new UserAdminQueryDto 
            {
                Search = filter.Search,
                Status = filter.Status,
                SortDescending = filter.SortDescending,
                Pagination = new PaginationParams
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize
                }
            });

            return View(new PagedResult<UserItemAdminViewModel>
            {
                Items = result.Items.Select(u => new UserItemAdminViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    Status = u.Status,
                }).ToList(),
                TotalItems = result.TotalItems,
                Page = result.Page,
            });
        }

        [HttpGet]
        public async Task<IActionResult> Products([FromQuery] ProductAdminFilterViewModel filter) 
        {
            var result = await _adminService.GetFilteredProductsAsync(new ProductAdminQueryDto
            {
                CategoryId = filter.CategoryId,
                Search = filter.Search,
                SortOption = filter.SortOption,
                Pagination = new PaginationParams
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize
                }
            });

            return View(new PagedResult<ProductItemAdminViewModel>
            {
                Items = result.Items.Select(p => new ProductItemAdminViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    Status = p.Status,
                }).ToList(),
                TotalItems = result.TotalItems,
                Page = result.Page
            });
        }

        [HttpGet]
        public async Task<IActionResult> Businesses([FromQuery] BusinessAdminFilterViewModel filter) 
        {
            var result = await _adminService.GetFilteredBusinessesAsync(new BusinessAdminQueryParams 
            {
                Search = filter.Search,
                SortDescending = filter.SortDescending,
                Pagination = new PaginationParams
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize
                }
            });


            return View(new PagedResult<BusinessItemAdminViewModel>
            {
                Items = result.Items.Select(b => new BusinessItemAdminViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    LogoUrl = b.LogoUrl,
                    Owner = b.Owner,
                    Status = b.Status,
                }).ToList(),
                TotalItems = result.TotalItems,
                Page = result.Page
            });
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
