using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Marketplace.Web.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;

    public IndexModel(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public string DisplayName { get; private set; } = "користувачу";
    public string Email { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        DisplayName = user?.UserName ?? "користувачу";
        Email = user?.Email ?? string.Empty;
    }
}
