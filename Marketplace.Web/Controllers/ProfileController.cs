using Marketplace.BLL.DTOs;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly IOrderService _orderService;
        private readonly IWishlistService _wishlistService;
        private readonly IProductService _productService;
        private readonly IGenericRepository<Address> _addressRepository;
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IUserDiscountService _userDiscountService;

        public ProfileController(
            IProfileService profileService,
            IOrderService orderService,
            IWishlistService wishlistService,
            IProductService productService,
            IGenericRepository<Address> addressRepository,
            AppDbContext context,
            UserManager<User> userManager,
            IEmailSender emailSender,
            IUserDiscountService userDiscountService)
        {
            _profileService = profileService;
            _orderService = orderService;
            _wishlistService = wishlistService;
            _productService = productService;
            _addressRepository = addressRepository;
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _userDiscountService = userDiscountService;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _profileService.GetProfileAsync(userId!);

            if (profile == null)
            {
                return NotFound();
            }

            var orders = await _orderService.GetUserOrders(userId!);
            var addresses = await _addressRepository.FindAsync(a => a.UserId == userId);
            var wishlist = await _wishlistService.GetWishlistAsync(userId!);
            var discounts = await _userDiscountService.GetActiveDiscountsForUserAsync(userId!);

            
            var now = DateTime.UtcNow;
            var personalDiscounts = await _context.UserDiscounts
                .Where(ud => ud.UserId == userId && ud.StartDate <= now && ud.EndDate > now)
                .OrderByDescending(ud => ud.Percent)
                .Select(ud => new PersonalDiscountViewModel
                {
                    StoreName = ud.Business.StoreName,
                    Percent = ud.Percent,
                    EndDate = ud.EndDate
                })
                .ToListAsync();

            return View(new ProfileViewModel
            {
                UserId = userId!,
                Username = profile.Username,
                Email = profile.Email,
                Avatar = profile.Avatar,
                OrdersCount = orders.Count,
                AddressesCount = addresses.Count,
                WishlistCount = wishlist.TotalItems,
                DiscountsCount = discounts.Count,
                PersonalDiscounts = personalDiscounts
            });
        }


        [HttpGet]
        public async Task<IActionResult> Discounts()
        {
            var userId = _userManager.GetUserId(User);
            var discounts = await _userDiscountService.GetActiveDiscountsForUserAsync(userId!);
            return View(discounts);
        }

        [HttpGet]
        public async Task<IActionResult> Activity()
        {
            var userId = _userManager.GetUserId(User)!;
            var orders = await _orderService.GetUserOrders(userId);

            var orderActivity = orders
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .Take(20)
                .Select(o => new ActivityItemViewModel
                {
                    OrderId = o.Id,
                    Status = o.Status,
                    Timestamp = o.UpdatedAt ?? o.CreatedAt,
                    TotalAmount = o.TotalAmount
                })
                .ToList();

            var newProducts = await _productService.GetCataloguePageAsync(new CatalogueFilterDto
            {
                Sort = CatalogueSortOption.Newest,
                Page = 1,
                PageSize = 6
            });

            var recommended = await _productService.GetCataloguePageAsync(new CatalogueFilterDto
            {
                Sort = CatalogueSortOption.Popularity,
                Page = 1,
                PageSize = 6
            });

            var model = new NotificationsViewModel
            {
                OrderActivity = orderActivity,
                NewProducts = newProducts.Items.Select(MapNotificationProduct).ToList(),
                RecommendedProducts = recommended.Items.Select(MapNotificationProduct).ToList()
            };

            return View(model);
        }

        private static NotificationProductViewModel MapNotificationProduct(CataloguePageItemDto item) => new()
        {
            Id = item.Product.Id,
            Name = item.Product.Name,
            ImageUrl = item.Product.MainImageUrl,
            Price = item.Product.Price,
            CategoryName = item.Product.Category?.Name ?? "Без категорії"
        };

        [HttpGet]
        public async Task<IActionResult> Wishlist()
        {
            var userId = _userManager.GetUserId(User)!;
            var wishlist = await _wishlistService.GetWishlistAsync(userId);

            var items = new List<WishlistItemViewModel>();
            foreach (var item in wishlist.Items)
            {
                var product = await _productService.GetByIdAsync(item.ProductId);
                items.Add(new WishlistItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    MainImageUrl = item.MainImageUrl,
                    InStock = product is not null && product.Quantity > 0
                });
            }

            return View(new WishlistPageViewModel { Id = wishlist.Id, Items = items });
        }

        [HttpGet]
        public async Task<IActionResult> RecentlyViewed()
        {
            return View(new RecentlyViewedPageViewModel
            {
                ConsentAccepted = Marketplace.Web.Infrastructure.CookieConsent.IsAccepted(Request.Cookies),
                Items = await Marketplace.Web.Infrastructure.RecentlyViewedCookie.LoadAsync(_productService, Request.Cookies)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(Guid productId, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var result = await _wishlistService.AddToWishlistAsync(userId, productId);
            TempData[result.Succeeded ? "WishlistSuccess" : "WishlistError"] = result.Message;
            return RedirectToLocalUrlOrWishlist(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(Guid productId, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var result = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            TempData[result.Succeeded ? "WishlistSuccess" : "WishlistError"] = result.Message;
            return RedirectToLocalUrlOrWishlist(returnUrl);
        }

        private IActionResult RedirectToLocalUrlOrWishlist(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Wishlist));
        }

        [HttpGet]
        public async Task<IActionResult> Addresses()
        {
            var userId = _userManager.GetUserId(User);
            var addresses = await _addressRepository.FindAsync(a => a.UserId == userId);

            var model = addresses
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AddressViewModel
                {
                    Id = a.Id,
                    Country = a.Country,
                    City = a.City,
                    Street = a.Street,
                    IsDefault = a.IsDefault
                })
                .ToList();

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateAddress(string? returnUrl = null)
        {
            return View("AddressForm", new AddressFormViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAddress(AddressFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("AddressForm", model);
            }

            var userId = _userManager.GetUserId(User)!;

            if (model.IsDefault)
            {
                await ClearDefaultAddressAsync(userId);
            }

            var address = new Address
            {
                UserId = userId,
                Country = model.Country.Trim(),
                City = model.City.Trim(),
                Street = model.Street.Trim(),
                IsDefault = model.IsDefault
            };

            await _addressRepository.AddAsync(address);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            TempData["Success"] = "Адресу додано.";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpGet]
        public async Task<IActionResult> EditAddress(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _addressRepository.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address is null)
            {
                return NotFound();
            }

            return View("AddressForm", new AddressFormViewModel
            {
                Id = address.Id,
                Country = address.Country,
                City = address.City,
                Street = address.Street,
                IsDefault = address.IsDefault
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(AddressFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("AddressForm", model);
            }

            var userId = _userManager.GetUserId(User)!;
            var address = await _addressRepository.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);
            if (address is null)
            {
                return NotFound();
            }

            if (model.IsDefault && !address.IsDefault)
            {
                await ClearDefaultAddressAsync(userId);
            }

            address.Country = model.Country.Trim();
            address.City = model.City.Trim();
            address.Street = model.Street.Trim();
            address.IsDefault = model.IsDefault;

            _addressRepository.Update(address);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Адресу оновлено.";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _addressRepository.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address is null)
            {
                return NotFound();
            }

            address.IsDeleted = true;
            _addressRepository.Update(address);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Адресу видалено.";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(Guid id)
        {
            var userId = _userManager.GetUserId(User)!;
            var address = await _addressRepository.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address is null)
            {
                return NotFound();
            }

            await ClearDefaultAddressAsync(userId);
            address.IsDefault = true;
            _addressRepository.Update(address);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Основну адресу оновлено.";
            return RedirectToAction(nameof(Addresses));
        }

        private async Task ClearDefaultAddressAsync(string userId)
        {
            var currentDefaults = await _addressRepository.FindAsync(a => a.UserId == userId && a.IsDefault);
            foreach (var address in currentDefaults)
            {
                address.IsDefault = false;
                _addressRepository.Update(address);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _profileService.GetProfileAsync(userId!);

            var model = new ProfileUpdateViewModel
            {
                Username = profile!.Username,
                CurrentAvatarUrl = profile.Avatar
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                model.CurrentAvatarUrl = (await _profileService.GetProfileAsync(userId!))?.Avatar;
                return View(model);
            }

            var updateDto = new UpdateProfileDto
            {
                Username = model.Username,
                Avatar = model.Avatar
            };

            var result = await _profileService.UpdateProfileAsync(updateDto, userId!);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                model.CurrentAvatarUrl = (await _profileService.GetProfileAsync(userId!))?.Avatar;
                return View(model);
            }

            TempData["Success"] = "Профіль оновлено.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangeEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var result = await _profileService.ChangeEmailAsync(new ChangeEmailDto { NewEmail = model.NewEmail, CurrentPassword = model.CurrentPassword }, userId!);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            var changeEmailUrl = Url.Action("ChangeEmailConfirmation", "Profile", new { userId = result.UserId, newEmail = result.NewEmail, token = result.Token }, Request.Scheme);
            await _emailSender.SendEmailAsync(model.NewEmail, "Підтвердження зміни емейлу", $"Будь ласка підтвердіть зміну емейла по цьому посиланню: <a href='{changeEmailUrl}'>link</a>");

            return RedirectToAction("ChangeEmailConfirmationSent");
        }

        [HttpGet]
        public IActionResult ChangeEmailConfirmationSent()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ChangeEmailConfirmation(string userId, string newEmail, string token)
        {
            var result = await _profileService.ConfirmChangeEmail(userId, newEmail, token);

            if (result.Succeeded)
            {
                ViewData["Message"] = "Емейл було успішно змінено!";
            }
            else
            {
                ViewData["Message"] = result.Message;
            }

            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var result = await _profileService.ChangePasswordAsync(new ChangePasswordDto
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmNewPassword = model.ConfirmNewPassword
            }, userId!);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUserId() 
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                return NotFound();
            }
            return Ok(new { UserId = userId }); 
        }
    }
}