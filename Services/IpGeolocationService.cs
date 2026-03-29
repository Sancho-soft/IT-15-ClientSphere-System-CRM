using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClientSphere.Services
{
    public interface IIpGeolocationService
    {
        Task LogIpLocationAsync(string ipAddress, string userEmail);
    }

    public class IpGeolocationService : IIpGeolocationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IpGeolocationService> _logger;

        public IpGeolocationService(HttpClient httpClient, ILogger<IpGeolocationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task LogIpLocationAsync(string ipAddress, string userEmail)
        {
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
            {
                _logger.LogInformation("[SECURITY AUDIT] Localhost login for user {Email}", userEmail);
                return;
            }

            try
            {
                var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<IpApiResponse>(json);

                    if (data?.Status == "success")
                    {
                        var msg = $"User {userEmail} logged in from {data.City}, {data.RegionName}, {data.Country} (IP: {ipAddress}, ISP: {data.Isp})";
                        _logger.LogWarning("[SECURITY AUDIT] " + msg);
                    }
                    else
                    {
                        _logger.LogInformation("[SECURITY AUDIT] User {Email} logged in from IP: {Ip} (Geolocation failed)", userEmail, ipAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve IP Geolocation for {Ip}", ipAddress);
            }
        }

        private class IpApiResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string? Status { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("country")]
            public string? Country { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("regionName")]
            public string? RegionName { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("city")]
            public string? City { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("isp")]
            public string? Isp { get; set; }
        }
    }
}
