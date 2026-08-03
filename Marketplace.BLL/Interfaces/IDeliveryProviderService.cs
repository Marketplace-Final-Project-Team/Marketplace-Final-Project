using System;
using System.Collections.Generic;
using System.Text;
using Marketplace.BLL.DTOs.Delivery;
namespace Marketplace.BLL.Interfaces
{
    public interface IDeliveryProviderService
    {
        
        Task<IEnumerable<DeliveryCityDto>> ListCitiesAsync(string? search = null, string? limit = null,string? page = null);
        Task<IEnumerable<DeliveryBranchDto>> ListBranchesAsync(string cityId, string? search = null, string? limit = null, string? typeofwarehouseref = null, string page = null);
    }
}
