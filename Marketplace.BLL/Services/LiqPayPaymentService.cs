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
        private readonly ISystemSettingsService _settingsService;

        public LiqPayPaymentService(IOptions<LiqPayOptions> options, ISystemSettingsService settingsService)
        {
            _options = options;
            _settingsService = settingsService;
        }

        // The admin Integrations page can override any of these in SystemSettings; an
        // admin-saved value always wins, falling back to appsettings.json when nothing's
        // been saved yet (see AdminController.BuildIntegrationsAsync for the same pattern).
        private async Task<string> GetSettingAsync(string key, string fallback)
        {
            var overridden = await _settingsService.GetAsync("LiqPay", key);
            return string.IsNullOrWhiteSpace(overridden) ? fallback : overridden;
        }

        public Task<string> GetPrivateKeyAsync() => GetSettingAsync("PrivateKey", _options.Value.PrivateKey);

        public async Task<PaymentDataDto> CreatePaymentAsync(string orderId,decimal amount,string description,string successUrl,string cancelUrl) {
            var publicKey = await GetSettingAsync("PublicKey", _options.Value.PublicKey);
            var privateKey = await GetPrivateKeyAsync();
            var serverUrl = await GetSettingAsync("ServerUrl", _options.Value.ServerUrl);

            var request = new
            {
                version = "3",
                public_key = publicKey,
                action = "pay",
                amount = amount,
                currency = "UAH",
                description = description,
                order_id = orderId,
                result_url = successUrl,
                server_url = serverUrl

            };
            string json_string = JsonSerializer.Serialize(request);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json_string);
            string base64Data = Convert.ToBase64String(jsonBytes);

            string signature = privateKey + base64Data + privateKey;
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
