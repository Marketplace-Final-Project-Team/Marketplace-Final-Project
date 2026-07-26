using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public  interface IBusinessService
    {
        Task<ServiceResult> CreateBusiness(BusinessCreateDto dto);
    }
}
