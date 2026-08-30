using Marketplace.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDataDto> CreatePaymentAsync(string orderId, decimal amount, string description, string successUrl, string cancelUrl);

        // Callback signature verification needs the exact same private key that signed the
        // payment, so it goes through the same admin-override lookup as CreatePaymentAsync
        // instead of reading appsettings.json directly.
        Task<string> GetPrivateKeyAsync();
    }
}
