using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Marketplace.BLL.Interfaces;

namespace Marketplace.BLL.Services
{
    public class ImageStorageService: IImageStorageService
    {
        private readonly Cloudinary _cloudinary;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public ImageStorageService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension)) return null;

            if (file.Length > MaxFileSizeBytes) return null;
            
            try
            {
                await using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    UseFilename = true,
                    UniqueFilename = true,
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null) return null;
                return result.SecureUrl.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
