using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Marketplace.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthService(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<SignUpResult> SignUp(SignUpDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
            {
                return new SignUpResult { Succeeded = false, Message = "Емейл вже використовується" };
            }

            var newUser = new User { UserName = dto.Username, Email = dto.Email };
            var result = await _userManager.CreateAsync(newUser, dto.Password);

            if (result.Succeeded == false)
            {
                return new SignUpResult { Succeeded = false, Message = string.Join("\n", result.Errors.Select(e => e.Description)) };
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);

            return new SignUpResult { Succeeded = true, Message = string.Empty, UserId = newUser.Id, Token = token };
        }

        public async Task<ServiceResult> SignIn(SignInDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null && user.EmailConfirmed == true)
            {
                var result = await _signInManager.PasswordSignInAsync(user, dto.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return new ServiceResult { Succeeded = true, Message = string.Empty };
                }
            }

            return new ServiceResult { Succeeded = false, Message = "Невірний емейл або пароль" };
        }

        public async Task<ServiceResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувач не знайдений" };
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                return new ServiceResult { Succeeded = true, Message = string.Empty };
            }

            return new ServiceResult { Succeeded = false, Message = "Не вдалося підтвердити емейл" };
        }

        public async Task<ForgotPasswordResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if(user == null)
            {
                return new ForgotPasswordResult
                {
                    Succeeded = true,
                    AccountExists = false,
                    Message = string.Empty
                };
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            return new ForgotPasswordResult
            {
                Succeeded = true,
                AccountExists = true,
                UserId = user.Id,
                Token = token,
                Message = string.Empty
            };
        }

        public async Task<ServiceResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Ви ще не маєте облікового запису." };
            }

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
                return new ServiceResult { Succeeded = false, Message = string.Join("\n", result.Errors.Select(e => e.Description)) };

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }
    }
}
