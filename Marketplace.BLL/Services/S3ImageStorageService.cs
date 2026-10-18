using Amazon.S3;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Marketplace.BLL.Services
{
    public class S3ImageStorageService: IImageStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeBytes = 100 * 1024 * 1024;
        private readonly ILogger<S3ImageStorageService> _logger;

        public S3ImageStorageService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3ImageStorageService> logger)
        {
            _s3Client = s3Client;
            _bucketName = configuration["AWS:S3BucketName"] ?? throw new InvalidOperationException("AWS:S3BucketName не налаштовано");
            _logger = logger;
        }

        public async Task<string?> UploadImageAsync(IFormFile file) 
        {
            if(file == null || file.Length == 0) return null;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if(!AllowedExtensions.Contains(extension)) return null;

            if(file.Length > MaxFileSizeBytes) return null;

            try 
            {
                var key = $"photos/{Guid.NewGuid()}{extension}";

                await using var stream = file.OpenReadStream();

                var putRequest = new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                var response = await _s3Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    return null;
                }

                return $"https://{_bucketName}.s3.amazonaws.com/{key}";
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "S3 помилка: {Code} / {Message}", ex.ErrorCode, ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не вдалося завантажити {File}", file.FileName);
                return null;
            }

        }
    }
}
