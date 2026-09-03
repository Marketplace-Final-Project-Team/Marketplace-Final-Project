using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Marketplace.BLL.Interfaces;

namespace Marketplace.BLL.Services
{
    public class ImageStorageService: IImageStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ISystemSettingsService _settingsService;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public ImageStorageService(IConfiguration configuration, ISystemSettingsService settingsService)
        {
            _configuration = configuration;
            _settingsService = settingsService;
        }

        private async Task<Cloudinary> BuildClientAsync()
        {
            var cloudName = await GetSettingAsync("CloudName", _configuration["Cloudinary:CloudName"] ?? "not-configured");
            var apiKey = await GetSettingAsync("ApiKey", _configuration["Cloudinary:ApiKey"] ?? "not-configured");
            var apiSecret = await GetSettingAsync("ApiSecret", _configuration["Cloudinary:ApiSecret"] ?? "not-configured");

            return new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        }

        private async Task<string> GetSettingAsync(string key, string fallback)
        {
            var overridden = await _settingsService.GetAsync("Cloudinary", key);
            return string.IsNullOrWhiteSpace(overridden) ? fallback : overridden;
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

                var cloudinary = await BuildClientAsync();
                var result = await cloudinary.UploadAsync(uploadParams);

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
