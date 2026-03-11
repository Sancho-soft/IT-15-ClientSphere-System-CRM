using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClientSphere.Services
{
    public interface ISystemSettingService
    {
        Task<string> GetSettingAsync(string key, string defaultValue);
        Task<int> GetSettingIntAsync(string key, int defaultValue);
        Task<bool> GetSettingBoolAsync(string key, bool defaultValue);
        Task SetSettingAsync(string key, string value, string group = "General");
        Task SetSettingIntAsync(string key, int value, string group = "General");
        Task SetSettingBoolAsync(string key, bool value, string group = "General");
        Task<Dictionary<string, string>> GetAllSettingsAsync();
    }
}
