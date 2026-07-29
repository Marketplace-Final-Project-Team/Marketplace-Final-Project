using Marketplace.Domain.Entities;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers;

public class AuthController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(SignIn));
    }

    [AllowAnonymous]
    [HttpGet("/login")]
    public IActionResult SignIn(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.Clear();
        return View(new SignInViewModel
        {
            ReturnUrl = NormalizeReturnUrl(returnUrl)
        });
    }

    [AllowAnonymous]
    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignIn(SignInViewModel model)
    {
        model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);
        ModelState.Remove(nameof(model.ReturnUrl));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Неправильна електронна пошта або пароль.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(model.ReturnUrl!);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Забагато невдалих спроб. Спробуйте ще раз через 10 хвилин.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Неправильна електронна пошта або пароль.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet("/register")]
    public IActionResult SignUp(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.Clear();
        return View(new SignUpViewModel
        {
            ReturnUrl = NormalizeReturnUrl(returnUrl)
        });
    }

    [AllowAnonymous]
    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(SignUpViewModel model)
    {
        model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);
        ModelState.Remove(nameof(model.ReturnUrl));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new User
        {
            UserName = model.Username.Trim(),
            Email = model.Email.Trim()
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
            }

            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(model.ReturnUrl!);
    }

    [Authorize]
    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(SignIn));
    }

    private string NormalizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Home")!;
    }

    private static string TranslateIdentityError(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateEmail" => "Користувач із такою електронною поштою вже існує.",
            "DuplicateUserName" => "Це ім’я користувача вже зайняте.",
            "InvalidEmail" => "Введіть коректну електронну пошту.",
            "InvalidUserName" => "Ім’я користувача містить недопустимі символи.",
            "PasswordTooShort" => "Пароль має містити щонайменше 8 символів.",
            "PasswordRequiresDigit" => "Додайте до пароля хоча б одну цифру.",
            "PasswordRequiresLower" => "Додайте до пароля малу літеру.",
            "PasswordRequiresUpper" => "Додайте до пароля велику літеру.",
            "PasswordRequiresNonAlphanumeric" => "Додайте до пароля спеціальний символ.",
            _ => error.Description
        };
    }
}
