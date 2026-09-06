using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.Interfaces
{
    public interface IUserDiscountService
    {
        Task<ServiceResult> SetUserDiscount(string userId, Guid businessId, decimal percent, DateTime startDate, DateTime endDate);
        Task<ServiceResult> RemoveUserDiscount(string userId, Guid businessId);
        Task<UserDiscountGetDto> GetUserDiscountAsync(string userId, Guid businessId);
    }
}
