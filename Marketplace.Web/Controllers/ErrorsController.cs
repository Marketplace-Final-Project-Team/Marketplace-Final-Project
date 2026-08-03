using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers;

[AllowAnonymous]
public class ErrorsController : Controller
{
    [HttpGet("/forbidden")]
    public IActionResult Forbidden()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [HttpGet("/error/{statusCode:int}")]
    public IActionResult StatusCodePage(int statusCode)
    {
        Response.StatusCode = statusCode;

        return statusCode switch
        {
            StatusCodes.Status403Forbidden => View(nameof(Forbidden)),
            StatusCodes.Status404NotFound => View("NotFound"),
            _ => View("StatusCode", statusCode)
        };
    }
}
