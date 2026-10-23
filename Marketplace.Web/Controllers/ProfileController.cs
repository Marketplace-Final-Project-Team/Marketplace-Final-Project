using Marketplace.Web.Infrastructure;
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

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly IOrderService _orderService;
        private readonly IWishlistService _wishlistService;
        private readonly IProductService _productService;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IUserDiscountService _userDiscountService;
        private readonly IAddressService _addressService;

        public ProfileController(
            IProfileService profileService,
            IOrderService orderService,
            IWishlistService wishlistService,
            IProductService productService,
            UserManager<User> userManager,
            IEmailSender emailSender,
            IUserDiscountService userDiscountService,
            IAddressService addressService)
        {
            _profileService = profileService;
            _orderService = orderService;
            _wishlistService = wishlistService;
            _productService = productService;
            _userManager = userManager;
            _emailSender = emailSender;
            _userDiscountService = userDiscountService;
            _addressService = addressService;
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
            var addresses = await _addressService.GetUserAddressesAsync(userId!);
            var wishlist = await _wishlistService.GetWishlistAsync(userId!);
            var discounts = await _userDiscountService.GetActiveDiscountsForUserAsync(userId!);


            var now = DateTime.UtcNow;

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
                PersonalDiscounts = discounts
                .OrderByDescending(d => d.Percent)
                .Select(d => new PersonalDiscountViewModel
                {
                    StoreName = d.StoreName!,
                    Percent = d.Percent,
                    EndDate = d.EndDate!.Value
                })
                .ToList()
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
                NewProducts = newProducts.Items.Select(item => MapNotificationProduct(item, userId)).ToList(),
                RecommendedProducts = recommended.Items.Select(item => MapNotificationProduct(item, userId)).ToList()
            };

            return View(model);
        }

        private static NotificationProductViewModel MapNotificationProduct(CataloguePageItemDto item, string userId) => new()
        {
            Id = item.Product.Id,
            Name = item.Product.Name,
            ImageUrl = item.Product.MainImageUrl,
            Price = item.Product.GetDiscountedPrice(userId),
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
                    Price = product?.GetDiscountedPrice(userId) ?? item.Price,
                    OriginalPrice = product?.GetEffectiveDiscountPercent(userId) > 0 ? product.Price : null,
                    DiscountPercent = product?.GetEffectiveDiscountPercent(userId) ?? 0m,
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
                Items = await Marketplace.Web.Infrastructure.RecentlyViewedCookie.LoadAsync(_productService, Request.Cookies, _userManager.GetUserId(User))
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(Guid productId, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var result = await _wishlistService.AddToWishlistAsync(userId, productId);
            TempData[result.Succeeded ? "WishlistSuccess" : "WishlistError"] = this.L(result.Message);
            return RedirectToLocalUrlOrWishlist(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(Guid productId, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var result = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            TempData[result.Succeeded ? "WishlistSuccess" : "WishlistError"] = this.L(result.Message);
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

            var res = await _addressService.GetUserAddressesAsync(userId!);

            if (userId is null)
            {
                return NotFound();
            }

            return View(res.Select(a => new AddressViewModel 
            {
                Id = a.Id,
                Country = a.Country,
                City = a.City,
                Street = a.Street,
                IsDefault = a.IsDefault
            }));
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

            var res = await _addressService.CreateAsync(userId, new AddressCreateDto
            {
                Country = model.Country.Trim(),
                City = model.City.Trim(),
                Street = model.Street.Trim(),
                IsDefault = model.IsDefault
            });

            if (!res.Succeeded)
            {
                TempData["Error"] = this.L(res.Message);
                return View("AddressForm", model);
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            TempData["Success"] = this.L("Адресу додано.");
            return RedirectToAction(nameof(Addresses));
        }

        [HttpGet]
        public async Task<IActionResult> EditAddress(Guid id)
        {
            var userId = _userManager.GetUserId(User);

            var address = await _addressService.GetByIdAsync(id, userId!);
            
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

            var res = await _addressService.EditAsync(new AddressDto 
            { 
                Id = model.Id, 
                Country = model.Country, 
                City = model.City,
                Street = model.Street,
                IsDefault = model.IsDefault
            }, userId);

            if(!res.Succeeded) 
            {
                TempData["Error"] = this.L(res.Message);
                return View("AddressForm", model);
            }

            TempData["Success"] = this.L("Адресу оновлено.");
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var userId = _userManager.GetUserId(User);

            var res = await _addressService.RemoveAsync(id, userId!);

            if (!res.Succeeded)
            {
                TempData["Error"] = this.L(res.Message);
            }
            else 
            {
                TempData["Success"] = this.L("Адресу видалено.");
            }

            return RedirectToAction(nameof(Addresses));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(Guid id)
        {
            var userId = _userManager.GetUserId(User)!;

            var res = await _addressService.SetDefaultAddress(id, userId);

            if (!res.Succeeded)
            {
                TempData["Error"] = this.L(res.Message);
            }
            else
            {
                TempData["Success"] = this.L("Основну адресу оновлено.");
            }

            return RedirectToAction(nameof(Addresses));
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
                ModelState.AddModelError(string.Empty, this.L(result.Message));
                model.CurrentAvatarUrl = (await _profileService.GetProfileAsync(userId!))?.Avatar;
                return View(model);
            }

            TempData["Success"] = this.L("Профіль оновлено.");
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
                ModelState.AddModelError("", this.L(result.Message));
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
                ViewData["Message"] = this.L("Емейл було успішно змінено!");
            }
            else
            {
                ViewData["Message"] = this.L(result.Message);
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
                ModelState.AddModelError("", this.L(result.Message));
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