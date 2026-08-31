using System.Security.Claims;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IActivityLogService _activityLog;

    public AuthController(
        IAuthService authService,
        IEmailSender emailSender,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IActivityLogService activityLog)
    {
        _authService = authService;
        _emailSender = emailSender;
        _userManager = userManager;
        _signInManager = signInManager;
        _activityLog = activityLog;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(SignIn));

    [AllowAnonymous]
    [HttpGet("/login")]
    public IActionResult SignIn(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new SignInViewModel { ReturnUrl = NormalizeReturnUrl(returnUrl) });
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
        if (user is null || !user.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Неправильна електронна пошта або пароль, або email ще не підтверджено.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _activityLog.LogUserActionAsync(user.Id, "LoggedIn", "Успішний вхід у систему.");
            return LocalRedirect(model.ReturnUrl!);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Забагато невдалих спроб. Спробуйте ще раз через 10 хвилин.");
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

        return View(new SignUpViewModel { ReturnUrl = NormalizeReturnUrl(returnUrl) });
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

        var result = await _authService.SignUp(new SignUpDto
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await _activityLog.LogUserActionAsync(result.UserId!, "Registered", $"Реєстрація нового користувача ({model.Email.Trim()}).");

        var confirmationLink = Url.Action(
            nameof(ConfirmEmail),
            "Auth",
            new { userId = result.UserId, token = result.Token },
            Request.Scheme);

        await _emailSender.SendEmailAsync(
            model.Email.Trim(),
            "Підтвердження email",
            $"Підтвердьте email за посиланням: <a href='{confirmationLink}'>підтвердити</a>.");

        return RedirectToAction(nameof(RegistrationSuccess));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult RegistrationSuccess() => View();

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest();
        }

        var result = await _authService.ConfirmEmail(userId, token);
        ViewBag.Succeeded = result.Succeeded;
        ViewBag.Message = result.Succeeded
            ? "Email успішно підтверджено! Тепер можна увійти."
            : result.Message;
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("Email", "Введіть електронну пошту.");
            return View();
        }

        var result = await _authService.ForgotPassword(email.Trim());
        if (result.AccountExists)
        {
            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Auth",
                new { userId = result.UserId, token = result.Token },
                Request.Scheme);

            await _emailSender.SendEmailAsync(
                email.Trim(),
                "Скидання пароля",
                $"Щоб скинути пароль, перейдіть за посиланням: <a href='{resetLink}'>скинути пароль</a>.");
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest();
        }

        return View(new ResetPasswordViewModel { UserId = userId, Token = token });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ResetPassword(new ResetPasswordDto
        {
            UserId = model.UserId,
            Token = model.Token,
            NewPassword = model.NewPassword
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    [Authorize]
    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // The NameIdentifier claim disappears once SignOutAsync runs, so it must be
        // captured before signing out if we want to log who logged out.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _signInManager.SignOutAsync();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await _activityLog.LogUserActionAsync(userId, "LoggedOut", "Вихід із системи.");
        }

        return RedirectToAction(nameof(SignIn));
    }

    private string NormalizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Home")!;
    }
}
