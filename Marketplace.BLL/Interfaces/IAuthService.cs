using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult> SignIn(SignInDto dto);
        Task<ServiceResult> SignUp(SignUpDto dto);
    }
}
