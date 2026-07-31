using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using System.Runtime.CompilerServices;

namespace Marketplace.BLL.Interfaces
{
    public interface IProfileService
    {
        public Task<ProfileDto?> GetProfile(string userId);    
        Task<ServiceResult> UpdateProfile(UpdateProfileDto dto, string userId);
        Task<ChangeEmailResult> ChangeEmail(ChangeEmailDto dto, string userId);
        Task<ServiceResult> ConfirmChangeEmail(string userId, string newEmail, string token);
        Task<ServiceResult> ChangePassword(ChangePasswordDto dto, string userId);
    }
}
