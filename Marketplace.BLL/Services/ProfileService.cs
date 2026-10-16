using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Marketplace.BLL.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IImageStorageService _imageService;

        public ProfileService(UserManager<User> userManager, SignInManager<User> signInManager, IImageStorageService imageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _imageService = imageService;
        }

        public async Task<ProfileDto?> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return null;
            }

            return new ProfileDto
            {
                Username = user.UserName!,
                Email = user.Email!,
                Avatar = user.AvatarUrl
            };
        }

        public async Task<ServiceResult> UpdateProfileAsync(UpdateProfileDto dto, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не було знайдено" };
            }

            if (dto.Avatar != null)
            {
                var avatarUrl = await _imageService.UploadImageAsync(dto.Avatar);

                if (avatarUrl == null)
                {
                    return new ServiceResult { Succeeded = false, Message = "Не вдалося завантажити аватар" };
                }

                user.AvatarUrl = avatarUrl;

            }

            var usernameResult = await _userManager.SetUserNameAsync(user, dto.Username);
            if (!usernameResult.Succeeded)
            {
                return new ServiceResult { Succeeded = false, Message = "Не вдалося оновити ім'я користувача" };
            }

            await _signInManager.RefreshSignInAsync(user);
            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ChangeEmailResult> ChangeEmailAsync(ChangeEmailDto dto, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ChangeEmailResult { Succeeded = false, Message = "Користувача не було знайдено" };
            }

            var passwordCheck = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);

            if (!passwordCheck)
            {
                return new ChangeEmailResult { Succeeded = false, Message = "Невірний пароль" };
            }

            if (user.Email == dto.NewEmail)
            {
                return new ChangeEmailResult
                {
                    Succeeded = false,
                    Message = "Це вже ваша поточна електронна адреса."
                };
            }

            var existing = await _userManager.FindByEmailAsync(dto.NewEmail);
            if (existing != null)
            {
                return new ChangeEmailResult { Succeeded = false, Message = "Ця пошта вже використовується" };
            }

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, dto.NewEmail);

            return new ChangeEmailResult { Succeeded = true, Message = string.Empty, UserId = user.Id, Token = token, NewEmail = dto.NewEmail };
        }

        public async Task<ServiceResult> ConfirmChangeEmail(string userId, string newEmail, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не було знайдено" };
            }

            var result = await _userManager.ChangeEmailAsync(user, newEmail, token);

            if (!result.Succeeded)
            {
                return new ServiceResult { Succeeded = false, Message = "Не вдалося підтвердити електронну пошту" };
            }

            await _signInManager.RefreshSignInAsync(user);
            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto dto, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не було знайдено" };
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                return new ServiceResult { Succeeded = false, Message = "Не вдалося змінити пароль" };
            }

            await _signInManager.RefreshSignInAsync(user);
            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }
    }
}
