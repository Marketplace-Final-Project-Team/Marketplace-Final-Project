using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult> SignIn(SignInDto dto);
        Task<SignUpResult> SignUp(SignUpDto dto);
        Task<ServiceResult> ConfirmEmail(string userId, string token);
        Task<ForgotPasswordResult> ForgotPassword(string email);
        Task<ServiceResult> ResetPassword(ResetPasswordDto dto);
    }
}
