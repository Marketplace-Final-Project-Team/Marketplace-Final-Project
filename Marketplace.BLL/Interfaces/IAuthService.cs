using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<DataResult<string>> SignInAsync(SignInDto dto);
        Task<SignUpResult> SignUpAsync(SignUpDto dto);
        Task<ServiceResult> ConfirmEmailAsync(string userId, string token);
        Task<ForgotPasswordResult> ForgotPasswordAsync(string email);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
