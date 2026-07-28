using System.ComponentModel.DataAnnotations;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Marketplace.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public LoginModel(UserManager<User> userManager, SignInManager<User> signInManager)
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

        var user = await _userManager.FindByEmailAsync(Input.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Неправильна електронна пошта або пароль.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl!);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Забагато невдалих спроб. Спробуйте ще раз через 10 хвилин.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Неправильна електронна пошта або пароль.");
        return Page();
    }

    private string NormalizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Page("/Index")!;
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Введіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Перевірте формат електронної пошти.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Залишатися в системі")]
        public bool RememberMe { get; set; }
    }
}
