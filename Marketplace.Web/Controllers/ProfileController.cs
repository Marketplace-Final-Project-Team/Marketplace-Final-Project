using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
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
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public ProfileController(IProfileService profileService, UserManager<User> userManager, IEmailSender emailSender)
        {
            _profileService = profileService;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _profileService.GetProfile(userId!);

            if(profile == null)
            {
                return NotFound();
            }

            return View(new ProfileViewModel
            {
                Username = profile.Username,
                Email = profile.Email,
                Avatar = profile.Avatar
            });
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _profileService.GetProfile(userId!);

            var model = new ProfileUpdateViewModel
            {
                Username = profile!.Username
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            
            var updateDto = new UpdateProfileDto
            {
                Username = model.Username,
                Avatar = model.Avatar
            };

            var result = await _profileService.UpdateProfile(updateDto, userId!);
            
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }
            
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
            if(!ModelState.IsValid) 
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var result = await _profileService.ChangeEmail(new ChangeEmailDto { NewEmail = model.NewEmail, CurrentPassword = model.CurrentPassword }, userId!);

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

            if(result.Succeeded) 
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
            if(!ModelState.IsValid) 
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var result = await _profileService.ChangePassword(new ChangePasswordDto 
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmNewPassword = model.ConfirmNewPassword
            }, userId!);

            if(!result.Succeeded) 
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
