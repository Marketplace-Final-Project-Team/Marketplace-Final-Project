using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    public class CultureController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetCulture(string culture, string? returnUrl = null)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
