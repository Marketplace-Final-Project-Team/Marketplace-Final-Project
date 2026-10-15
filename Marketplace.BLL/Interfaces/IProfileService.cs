using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using System.Runtime.CompilerServices;

namespace Marketplace.BLL.Interfaces
{
    public interface IProfileService
    {
        public Task<ProfileDto?> GetProfileAsync(string userId);
        Task<ServiceResult> UpdateProfileAsync(UpdateProfileDto dto, string userId);
        Task<ChangeEmailResult> ChangeEmailAsync(ChangeEmailDto dto, string userId);
        Task<ServiceResult> ConfirmChangeEmail(string userId, string newEmail, string token);
        Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto dto, string userId);
    }
}
