using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Marketplace.Web.Infrastructure;

// Translates user-facing controller messages (TempData, model errors, JSON responses) through
// SharedResource. The key is the Ukrainian text; when no translation exists, the key itself is
// returned, so messages coming from services stay readable even if they are not in the resx yet.
public static class ControllerLocalization
{
    public static string L(this ControllerBase controller, string? key, params object?[] args)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key ?? string.Empty;
        }

        var localizer = controller.HttpContext?.RequestServices.GetService<IStringLocalizer<SharedResource>>();
        if (localizer is null)
        {
            return args.Length == 0 ? key : string.Format(key, args);
        }

        return args.Length == 0 ? localizer[key].Value : localizer[key, args!].Value;
    }
}
