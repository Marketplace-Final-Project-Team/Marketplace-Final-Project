using Marketplace.BLL.Interfaces;
using Marketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailSender _emailSender;

        public AuthController(IAuthService authService, IEmailSender emailSender)
        {
            _authService = authService;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.SignIn(new SignInDto { Email = model.Email, Password = model.Password });

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.SignUp(new SignUpDto
            {
                Username = model.Username,
                Email = model.Email,
                Password = model.Password
            });

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { userId = result.UserId, token = result.Token }, Request.Scheme);
            await _emailSender.SendEmailAsync(model.Email, "Підтвердження емейлу", $"Будь ласка підтвердіть ваш емейл по цьому посиланню: <a href='{confirmationLink}'>link</a>");

            return RedirectToAction("RegistrationSuccess");
        }

        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction("Index", "Auth");

            var result = await _authService.ConfirmEmail(userId, token);

            ViewBag.Message = result.Succeeded
                ? "Email успішно підтверджено! Тепер можна увійти."
                : result.Message;

            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("Email", "Будь ласка введіть емейл");
                return View();
            }
            var result = await _authService.ForgotPassword(email);

            if (!result.AccountExists)
            {
                await _emailSender.SendEmailAsync(email, "Скидання пароля", $"Облікового запису на ваш емейл не було знайдено.");
            }
            else
            {
                var resetLink = Url.Action("ResetPassword", "Auth", new { userId = result.UserId, token = result.Token }, Request.Scheme);
                await _emailSender.SendEmailAsync(email, "Скидання пароля", $"Щоб скинути пароль, перейдіть за цим посиланням: <a href='{resetLink}'>link</a>");
            }
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction("Index", "Auth");

            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }

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
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
