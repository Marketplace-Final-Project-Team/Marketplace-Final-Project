using Marketplace.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDataDto> CreatePaymentAsync(string orderId, decimal amount, string description, string successUrl, string cancelUrl);
    }
}
