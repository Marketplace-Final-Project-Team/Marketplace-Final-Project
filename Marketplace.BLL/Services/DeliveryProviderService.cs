using CloudinaryDotNet;
using Marketplace.BLL.Configs;
using Marketplace.BLL.DTOs.Delivery;
using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Marketplace.BLL.Services
{

    public class DeliveryProviderService : IDeliveryProviderService
    {
        private readonly DeliveryApiOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ISystemSettingsService _settingsService;


        public DeliveryProviderService(IOptions<DeliveryApiOptions> options, ISystemSettingsService settingsService)
        {
            _options = options.Value;
            _httpClient = new HttpClient();
            _settingsService = settingsService;
        }

        // The admin Integrations page can override the Nova Poshta key/URL in SystemSettings;
        // an admin-saved value always wins, falling back to appsettings.json when nothing's
        // been saved yet (see AdminController.BuildIntegrationsAsync for the same pattern).
        private async Task<string> GetSettingAsync(string key, string fallback)
        {
            var overridden = await _settingsService.GetAsync("NovaPoshta", key);
            return string.IsNullOrWhiteSpace(overridden) ? fallback : overridden;
        }

        public async Task<IEnumerable<DeliveryCityDto>> ListCitiesAsync(string? search = null, string? limit = null,string? page = null)
        {
            var apiKey = await GetSettingAsync("NovaPoshtaApiKey", _options.NovaPoshtaApiKey);
            var baseUrl = await GetSettingAsync("NovaPoshtaBaseUrl", _options.NovaPoshtaBaseUrl);

            // 1. Сформуй об'єкт запиту для Нової Пошти
            // 2. Серіалізуй у JSON
            // 3. Відправ POST запит
            // 4. Отримай відповідь
            // 5. Розпарси у List<DeliveryCityDto>
            // 6. Поверни результат
            var request = new
            {
                apiKey = apiKey,
                modelName = "Address",
                calledMethod = "getCities",
                methodProperties = new
                {
                    FindByString = search ?? "",
                    Limit = limit ?? "50",
                    Page = page ?? "1"
                }
            };
            var json_request = JsonSerializer.Serialize(request);

                var content = new StringContent(json_request, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(baseUrl, content);

                if (response.IsSuccessStatusCode)
                {

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonDocument = JsonDocument.Parse(responseContent);
                    var root = jsonDocument.RootElement;
                    if (root.TryGetProperty("data", out JsonElement dataElement))
                    {
                        var cities = new List<DeliveryCityDto>();
                        foreach (var cityElement in dataElement.EnumerateArray())
                        {
                            var city = new DeliveryCityDto
                            {
                                Id = cityElement.GetProperty("Ref").GetString(),
                                Name = cityElement.GetProperty("Description").GetString()
                            };
                            cities.Add(city);
                        }
                        return cities;
                    }



                }

                throw new Exception($"Failed to post data: {response.StatusCode}");
            
        }

        public async Task<IEnumerable<DeliveryBranchDto>> ListBranchesAsync(string cityId, string? search = null, string? limit = null, string? typeofwarehouseref = null, string page = null)
        {
            var apiKey = await GetSettingAsync("NovaPoshtaApiKey", _options.NovaPoshtaApiKey);
            var baseUrl = await GetSettingAsync("NovaPoshtaBaseUrl", _options.NovaPoshtaBaseUrl);

            // 1. Сформуй об'єкт запиту для Нової Пошти
            // 2. Серіалізуй у JSON
            // 3. Відправ POST запит
            // 4. Отримай відповідь
            // 5. Розпарси у List<DeliveryBranchDto>
            // 6. Поверни результат
            var request = new
            {
                apiKey = apiKey,
                modelName = "Address",
                calledMethod = "getWarehouses",
                methodProperties = new
                {
                    CityRef = cityId,
                    FindByString = search ?? "",
                    Limit = limit ?? "50",
                    TypeOfWarehouseRef = typeofwarehouseref ?? "",
                    Page = page ?? "1",
                    Language = "UA"

                }
            };
            var json_request = JsonSerializer.Serialize(request);

            var content = new StringContent(json_request, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(baseUrl, content);

            if (response.IsSuccessStatusCode)
            {

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseContent);
                var root = jsonDocument.RootElement;
                if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    var warehouses = new List<DeliveryBranchDto>();
                    foreach (var cityElement in dataElement.EnumerateArray())
                    {
                        var warehouse = new DeliveryBranchDto
                        {
                            Id = cityElement.GetProperty("Ref").GetString(),
                            Name = cityElement.GetProperty("Description").GetString(),
                            Address = cityElement.GetProperty("ShortAddress").GetString()
                        };
                        warehouses.Add(warehouse);
                    }
                    return warehouses;
                }
                return new List<DeliveryBranchDto>();



            }

            throw new Exception($"Failed to post data: {response.StatusCode}");
        }
        public async Task<IEnumerable<WarehouseTypeDto>> GetWarehouseTypesAsync()
        {
            var apiKey = await GetSettingAsync("NovaPoshtaApiKey", _options.NovaPoshtaApiKey);
            var baseUrl = await GetSettingAsync("NovaPoshtaBaseUrl", _options.NovaPoshtaBaseUrl);

            var request = new
            {
                apiKey = apiKey,
                modelName = "Address",
                calledMethod = "getWarehouseTypes",
                methodProperties = new { }
            };

            var json_request = JsonSerializer.Serialize(request);
            var content = new StringContent(json_request, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(baseUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseContent);
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    var types = new List<WarehouseTypeDto>();
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        types.Add(new WarehouseTypeDto
                        {
                            Id = item.GetProperty("Ref").GetString(),
                            Name = item.GetProperty("Description").GetString()
                        });
                    }
                    return types;
                }
                return new List<WarehouseTypeDto>();
            }

            throw new Exception($"Failed to get warehouse types: {response.StatusCode}");
        }

    }

}
