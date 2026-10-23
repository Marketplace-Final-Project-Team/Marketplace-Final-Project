using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Marketplace.Web.Models
{
    // [Range(typeof(bool), "true", "true")] looks like it should work for "must be checked"
    // checkboxes, but jQuery Unobtrusive Validation's range adapter runs parseFloat() on the
    // element value, which is always NaN for "true"/"false" — so the client-side check fails
    // permanently, even when the box is checked. This attribute validates the same rule but
    // ships its own "mustbetrue" client adapter (registered in SignUp.cshtml) that reads
    // element.checked directly, so client and server agree.
    public sealed class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
    {
        public MustBeTrueAttribute()
        {
            ErrorMessage ??= "Це поле потрібно підтвердити.";
        }

        public override bool IsValid(object? value) => value is bool flag && flag;

        public void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            var localizer = context.ActionContext.HttpContext.RequestServices.GetService<IStringLocalizer<SharedResource>>();
            var message = localizer is null || string.IsNullOrEmpty(ErrorMessage)
                ? FormatErrorMessage(context.ModelMetadata.GetDisplayName())
                : localizer[ErrorMessage].Value;
            MergeAttribute(context.Attributes, "data-val-mustbetrue", message);
        }

        private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (!attributes.ContainsKey(key))
            {
                attributes[key] = value;
            }
        }
    }
}
