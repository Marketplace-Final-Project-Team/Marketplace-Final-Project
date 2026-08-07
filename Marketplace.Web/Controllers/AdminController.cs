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

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Users([FromQuery] UserAdminFilterViewModel filter) 
        {
            var result = await _adminService.GetUsersAsync(new UserAdminQueryDto 
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
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            });
        }

        [HttpGet]
        public async Task<IActionResult> Products([FromQuery] ProductAdminFiterViewModel filter) 
        {
            var result = await _adminService.GetProductsAsync(new ProductAdminQueryDto
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
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize  
            });
        }

        [HttpGet]
        public async Task<IActionResult> Businesses([FromQuery] BusinessAdminFilterViewModel filter) 
        {
            var result = await _adminService.GetBusinessesAsync(new BusinessAdminQueryParams 
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
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
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
