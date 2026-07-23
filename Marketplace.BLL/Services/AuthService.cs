using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Marketplace.BLL.Services
{
    public class AuthService: IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthService(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<ServiceResult> SignUp(SignUpDto dto) 
        {
            if(await _userManager.FindByEmailAsync(dto.Email) != null)
            {
                return new ServiceResult { Succeeded = false, Message = "Емейл вже використовується" };
            }

            var result = await _userManager.CreateAsync(new User { UserName = dto.Username, Email = dto.Email }, dto.Password);

            if(result.Succeeded == false)
            {
                return new ServiceResult { Succeeded = false, Message = string.Join("\n", result.Errors.Select(e => e.Description)) };
            }

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> SignIn(SignInDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null) 
            {
                var result = await _signInManager.PasswordSignInAsync(user, dto.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return new ServiceResult { Succeeded = true, Message = string.Empty };
                }
            }

            return new ServiceResult { Succeeded = false, Message = "Невірний емейл або пароль" };
        }
    }
}
