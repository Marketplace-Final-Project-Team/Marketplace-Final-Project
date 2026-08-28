using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers
{
    public class AgeVerificationController : Controller
    {
        [HttpPost]
        public IActionResult ConfirmAge([FromForm] string returnUrl)
        {
            HttpContext.Session.SetString("IsAdultConfirmed", "true");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}