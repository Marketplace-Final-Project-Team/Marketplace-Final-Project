using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Services;
using Microsoft.AspNetCore.Mvc;
namespace Marketplace.Web.Controllers
{
    public class DeliveryController : Controller
    {
        
        private readonly IDeliveryProviderService _deliveryProvider;

        public DeliveryController(IDeliveryProviderService deliveryProvider)
        {
            _deliveryProvider = deliveryProvider;
        }

        [HttpGet]
        public async Task<IActionResult> Cities(string? search = null, string? limit = null, string? page = null)
        {
            try
            {
                var cities = await _deliveryProvider.ListCitiesAsync(search, limit, page);
                return Ok(cities);
            }
            catch (Exception)
            {
                // Nova Poshta's API being slow/unreachable shouldn't crash the checkout page —
                // the city picker just shows "нічого не знайдено" instead of a 500.
                return Ok(Array.Empty<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Branches(string cityId, string? search = null, string? limit = null, string? typeofwarehouseref = null, string page = null)
        {
            try
            {
                var branches = await _deliveryProvider.ListBranchesAsync(cityId, search, limit, typeofwarehouseref, page);
                return Ok(branches);
            }
            catch (Exception)
            {
                return Ok(Array.Empty<object>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetWarehouseTypes()
        {
            Console.WriteLine("GetWarehouseTypesAsync called");
            if (_deliveryProvider is DeliveryProviderService npService)
            {
                var types = await npService.GetWarehouseTypesAsync();
                return Ok(types);
            }
            return BadRequest("This delivery service does not support warehouse types.");
        }

    }
}
