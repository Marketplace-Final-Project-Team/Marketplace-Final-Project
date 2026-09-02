using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Models.Products
{
    public class ProductCreateViewModel : IValidatableObject
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private const int MaxAdditionalImageCount = 8;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        [Required(ErrorMessage = "Оберіть бізнес")]
        public Guid BusinessId { get; set; }

        [Required(ErrorMessage = "Вкажіть назву товару")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Додайте опис товару")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ціна має бути більшою за 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Кількість не може бути від'ємною")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Оберіть категорію")]
        public Guid CategoryId { get; set; }

        [Display(Name = "Товар тільки для дорослих (18+)")]
        public bool IsAdultOnly { get; set; }

 

        public IFormFile? MainImage { get; set; }
        public List<IFormFile>? AdditionalImages { get; set; }
        public IEnumerable<SelectListItem> Businesses { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MainImage is not null)
            {
                var mainImageError = ValidateImage(MainImage);
                if (mainImageError is not null)
                {
                    yield return new ValidationResult(mainImageError, new[] { nameof(MainImage) });
                }
            }

            if (AdditionalImages is null)
            {
                yield break;
            }

            if (AdditionalImages.Count > MaxAdditionalImageCount)
            {
                yield return new ValidationResult(
                    $"Можна додати не більше {MaxAdditionalImageCount} додаткових зображень.",
                    new[] { nameof(AdditionalImages) });
            }

            foreach (var image in AdditionalImages)
            {
                var imageError = ValidateImage(image);
                if (imageError is not null)
                {
                    yield return new ValidationResult($"{image.FileName}: {imageError}", new[] { nameof(AdditionalImages) });
                }
            }
        }

        private static string? ValidateImage(IFormFile image)
        {
            if (image.Length == 0)
            {
                return "файл порожній.";
            }

            if (image.Length > MaxImageSizeBytes)
            {
                return "розмір зображення перевищує 5 МБ.";
            }

            var extension = Path.GetExtension(image.FileName);
            if (!AllowedImageExtensions.Contains(extension) || !AllowedImageContentTypes.Contains(image.ContentType))
            {
                return "підтримуються лише JPG, PNG і WEBP.";
            }

            return null;
        }
    }
}
