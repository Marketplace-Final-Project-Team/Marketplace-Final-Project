using Marketplace.BLL.Configs;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Marketplace.BLL.Services
{
    public class LiqPayPaymentService : IPaymentService
    {
        private readonly IOptions<LiqPayOptions> _options;
        private readonly ISystemSettingsService _settingsService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<LiqPayPaymentService> _logger;
        private readonly IWebHostEnvironment _environment;

        public LiqPayPaymentService(
            IOptions<LiqPayOptions> options,
            ISystemSettingsService settingsService,
            HttpClient httpClient,
            ILogger<LiqPayPaymentService> logger,
            IWebHostEnvironment environment)
        {
            _options = options;
            _settingsService = settingsService;
            _httpClient = httpClient;
            _logger = logger;
            _environment = environment;
        }

        private async Task<string> GetSettingAsync(string key, string fallback)
        {
            var overridden = await _settingsService.GetAsync("LiqPay", key);
            return string.IsNullOrWhiteSpace(overridden) ? fallback : overridden;
        }

        public Task<string> GetPrivateKeyAsync() => GetSettingAsync("PrivateKey", _options.Value.PrivateKey);

        public async Task<PaymentDataDto> CreatePaymentAsync(string orderId, decimal amount, string description, string successUrl, string cancelUrl)
        {
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
            string base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json_string));

            string signature = privateKey + base64Data + privateKey;
            string base64signature = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(signature)));

            return new PaymentDataDto
            {
                Data = base64Data,
                Signature = base64signature,
                Url = "https://www.liqpay.ua/api/3/checkout"
            };
        }

        public async Task<bool> RefundPaymentAsync(string orderId, decimal amount)
        {
            var publicKey = await GetSettingAsync("PublicKey", _options.Value.PublicKey);
            var privateKey = await GetPrivateKeyAsync();

            var request = new
            {
                version = "3",
                public_key = publicKey,
                action = "refund",
                amount = amount,
                currency = "UAH",
                order_id = orderId
            };

            string json_string = JsonSerializer.Serialize(request);
            string base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json_string));

            string signature = privateKey + base64Data + privateKey;
            string base64signature = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(signature)));

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", base64Data),
                new KeyValuePair<string, string>("signature", base64signature)
            });

            try
            {
                var httpResponse = await _httpClient.PostAsync("https://www.liqpay.ua/api/request", formContent);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("LiqPay Refund HTTP Error: {StatusCode}", httpResponse.StatusCode);
                    return false;
                }

                string jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("LiqPay Refund Response: {Response}", jsonResponse);

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp))
                {
                    string? status = statusProp.GetString();

                    if (status == "success" || status == "wait_accept" || status == "reversed")
                    {
                        return true;
                    }
                }

                if (_environment.IsDevelopment())
                {
                    _logger.LogWarning(
                        "LiqPay rejected refund for order {OrderId}. Bypassing ONLY because environment is Development " +
                        "(expected for test orders without a real sandbox payment). This bypass is disabled in Production. Response: {Response}",
                        orderId, jsonResponse);
                    return true;
                }

                _logger.LogError(
                    "LiqPay refund rejected for order {OrderId} in {Environment} environment. Response: {Response}",
                    orderId, _environment.EnvironmentName, jsonResponse);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while processing LiqPay refund for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> CreatePayoutAsync(
            Guid requestId,
            decimal amount,
            string cardNumber,
            string recipientName,
            string description
            )
        {
            var publicKey = await GetSettingAsync("PublicKey", _options.Value.PublicKey);
            var privateKey = await GetPrivateKeyAsync();
            var serverUrl = await GetSettingAsync("ServerUrl", _options.Value.ServerUrl);

            var request = new
            {
                version = "3",
                public_key = publicKey,
                action = "payout",
                card = cardNumber,
                receiver_name = recipientName,
                amount = amount,
                currency = "UAH",
                description = description,
                order_id = requestId.ToString(),
                server_url = serverUrl
            };

            var json = JsonSerializer.Serialize(request);
            string base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            string signature = privateKey + base64Data + privateKey;
            string base64signature = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(signature)));

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", base64Data),
                new KeyValuePair<string, string>("signature", base64signature)
            });

            try
            {
                var httpResponse = await _httpClient.PostAsync("https://www.liqpay.ua/api/request", formContent);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("LiqPay Payout HTTP Error: {StatusCode}", httpResponse.StatusCode);
                    return false;
                }

                string jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("LiqPay Payout Response: {Response}", jsonResponse);

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp))
                {
                    string? status = statusProp.GetString();

                    if (status == "success" || status == "wait_accept")
                    {
                        return true;
                    }
                }

                _logger.LogError("LiqPay payout rejected for request {RequestId}. Response: {Response}", requestId, jsonResponse);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while processing LiqPay payout for request {RequestId}", requestId);
                return false;
            }
        }
    }
}