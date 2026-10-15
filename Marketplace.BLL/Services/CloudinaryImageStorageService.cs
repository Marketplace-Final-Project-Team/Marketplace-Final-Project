using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Marketplace.BLL.Interfaces;

namespace Marketplace.BLL.Services
{
    public class CloudinaryImageStorageService : IImageStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryImageStorageService> _logger;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public CloudinaryImageStorageService(Cloudinary cloudinary, ILogger<CloudinaryImageStorageService> logger)
        {
            _cloudinary = cloudinary;
            _logger = logger;
        }

        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Cloudinary: файл порожній або відсутній.");
                return null;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Cloudinary: недозволене розширення '{Extension}' ({FileName}).", extension, file.FileName);
                return null;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                _logger.LogWarning("Cloudinary: файл {FileName} завеликий ({Size} байт).", file.FileName, file.Length);
                return null;
            }

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

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary відхилив {FileName}: {Error} (HTTP {Status})",
                        file.FileName, result.Error.Message, result.StatusCode);
                    return null;
                }

                return result.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary: виняток під час завантаження {FileName}.", file.FileName);
                return null;
            }
        }
    }
}