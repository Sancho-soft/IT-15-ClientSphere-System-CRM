using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClientSphere.Data;
using ClientSphere.Models;

namespace ClientSphere.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        public async Task<int> GetSettingIntAsync(string key, int defaultValue)
        {
            var valueStr = await GetSettingAsync(key, defaultValue.ToString());
            return int.TryParse(valueStr, out int result) ? result : defaultValue;
        }

        public async Task<bool> GetSettingBoolAsync(string key, bool defaultValue)
        {
            var valueStr = await GetSettingAsync(key, defaultValue.ToString());
            return bool.TryParse(valueStr, out bool result) ? result : defaultValue;
        }

        public async Task SetSettingAsync(string key, string value, string group = "General")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Group = group,
                    LastUpdatedAt = DateTime.UtcNow
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.Group = group;
                setting.LastUpdatedAt = DateTime.UtcNow;
                _context.SystemSettings.Update(setting);
            }
            await _context.SaveChangesAsync();
        }

        public async Task SetSettingIntAsync(string key, int value, string group = "General")
        {
            await SetSettingAsync(key, value.ToString(), group);
        }

        public async Task SetSettingBoolAsync(string key, bool value, string group = "General")
        {
            await SetSettingAsync(key, value.ToString(), group);
        }

        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            return await _context.SystemSettings
                .ToDictionaryAsync(s => s.Key, s => s.Value);
        }
    }
}
