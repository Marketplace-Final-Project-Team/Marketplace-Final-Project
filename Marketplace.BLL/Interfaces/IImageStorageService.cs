using Microsoft.AspNetCore.Http;

namespace Marketplace.BLL.Interfaces
{
    public interface IImageStorageService
    {
        Task<string?> UploadImageAsync(IFormFile file);
    }
}
