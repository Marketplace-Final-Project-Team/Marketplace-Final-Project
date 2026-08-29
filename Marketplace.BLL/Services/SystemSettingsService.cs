using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace Marketplace.BLL.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private const string ProtectorPurpose = "Marketplace.SystemSettings.Secrets";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IDataProtector _protector;

        public SystemSettingsService(AppDbContext context, IMemoryCache cache, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _cache = cache;
            _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        }

        public async Task<string?> GetAsync(string category, string key)
        {
            var cacheKey = CacheKey(category, key);
            if (_cache.TryGetValue(cacheKey, out string? cached))
            {
                return cached;
            }

            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);

            var value = setting is null
                ? null
                : setting.IsSecret ? Unprotect(setting.Value) : setting.Value;

            _cache.Set(cacheKey, value, CacheTtl);
            return value;
        }

        public async Task<int> GetIntAsync(string category, string key, int fallback)
        {
            var raw = await GetAsync(category, key);
            return int.TryParse(raw, out var parsed) ? parsed : fallback;
        }

        public async Task<List<SystemSetting>> GetAllAsync()
        {
            var settings = await _context.SystemSettings
                .AsNoTracking()
                .OrderBy(s => s.Category).ThenBy(s => s.Key)
                .ToListAsync();

            return DecryptForDisplay(settings);
        }

        public async Task<List<SystemSetting>> GetByCategoryAsync(string category)
        {
            var settings = await _context.SystemSettings
                .AsNoTracking()
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();

            return DecryptForDisplay(settings);
        }

        public async Task SetAsync(string category, string key, string value, bool isSecret, string? actorUserId = null)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);

            var storedValue = isSecret ? _protector.Protect(value) : value;

            if (setting is null)
            {
                setting = new SystemSetting
                {
                    Category = category,
                    Key = key,
                    Value = storedValue,
                    IsSecret = isSecret
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = storedValue;
                setting.IsSecret = isSecret;
            }

            await _context.SaveChangesAsync();
            _cache.Remove(CacheKey(category, key));
        }

        public async Task SeedIfMissingAsync(string category, string key, string value, bool isSecret)
        {
            var exists = await _context.SystemSettings.AnyAsync(s => s.Category == category && s.Key == key);
            if (exists)
            {
                return;
            }

            _context.SystemSettings.Add(new SystemSetting
            {
                Category = category,
                Key = key,
                Value = isSecret ? _protector.Protect(value) : value,
                IsSecret = isSecret
            });
        }

        private List<SystemSetting> DecryptForDisplay(List<SystemSetting> settings)
        {
            foreach (var setting in settings.Where(s => s.IsSecret))
            {
                setting.Value = Unprotect(setting.Value);
            }

            return settings;
        }

        private string Unprotect(string value)
        {
            try
            {
                return _protector.Unprotect(value);
            }
            catch (CryptographicException)
            {
                return string.Empty;
            }
        }

        private static string CacheKey(string category, string key) => $"system-setting:{category}:{key}";
    }
}
