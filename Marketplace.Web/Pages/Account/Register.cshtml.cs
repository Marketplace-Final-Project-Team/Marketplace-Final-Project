using System.ComponentModel.DataAnnotations;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Marketplace.Web.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public RegisterModel(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        ReturnUrl = NormalizeReturnUrl(ReturnUrl);
        ModelState.Remove(nameof(ReturnUrl));
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl);
        ModelState.Remove(nameof(ReturnUrl));

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new User
        {
            UserName = Input.Username.Trim(),
            Email = Input.Email.Trim()
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
            }

            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(ReturnUrl!);
    }

    private string NormalizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Page("/Index")!;
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

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Введіть ім’я користувача.")]
        [StringLength(
            30,
            MinimumLength = 2,
            ErrorMessage = "Ім’я має містити від 2 до 30 символів.")]
        [RegularExpression(
            @"^[\p{L}\p{N}._-]+$",
            ErrorMessage = "Використовуйте літери, цифри, крапку, дефіс або підкреслення.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Перевірте формат електронної пошти.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Створіть пароль.")]
        [DataType(DataType.Password)]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Пароль не відповідає вимогам безпеки.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повторіть пароль.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Потрібно прийняти правила користування.")]
        public bool AcceptTerms { get; set; }
    }
}
