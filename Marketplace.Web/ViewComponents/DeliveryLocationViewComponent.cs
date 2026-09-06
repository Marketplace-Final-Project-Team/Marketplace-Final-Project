using System.Security.Claims;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Web.ViewComponents;

public class DeliveryLocationViewComponent : ViewComponent
{
    private readonly IGenericRepository<Address> _addressRepository;

    public DeliveryLocationViewComponent(IGenericRepository<Address> addressRepository)
    {
        _addressRepository = addressRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return View((Address?)null);
        }

        var addresses = await _addressRepository.FindAsync(a => a.UserId == userId);
        var address = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();

        return View(address);
    }
}
