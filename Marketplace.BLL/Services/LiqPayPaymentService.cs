using CloudinaryDotNet;
using Marketplace.BLL.Configs;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace Marketplace.BLL.Services
{
    public class LiqPayPaymentService : IPaymentService
    {
        public readonly IOptions<LiqPayOptions> _options;

        public LiqPayPaymentService(IOptions<LiqPayOptions> options)
        {
            _options = options;
        }

        public async Task<PaymentDataDto> CreatePaymentAsync(string orderId,decimal amount,string description,string successUrl,string cancelUrl) {
            var request = new
            {
                version = "3",
                public_key = _options.Value.PublicKey,
                action = "pay",
                amount = amount,
                currency = "UAH",
                description = description,
                order_id = orderId,
                result_url = successUrl,   
                server_url = _options.Value.ServerUrl

            };
            string json_string = JsonSerializer.Serialize(request);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json_string);
            string base64Data = Convert.ToBase64String(jsonBytes); 

            string signature = _options.Value.PrivateKey + base64Data + _options.Value.PrivateKey;
            byte[] inputBytes = Encoding.UTF8.GetBytes(signature);

           
            byte[] hashBytes = SHA1.HashData(inputBytes);
            string base64signature = Convert.ToBase64String(hashBytes);
            var response = new PaymentDataDto
            {
                Data = base64Data,
                Signature = base64signature,
                Url = "https://www.liqpay.ua/api/3/checkout"
            };  
            return response;

        }
    }
}
