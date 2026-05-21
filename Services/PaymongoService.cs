using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ClientSphere.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace ClientSphere.Services
{
    public interface IPaymongoService
    {
        Task<string> CreatePaymentLinkAsync(Invoice invoice, string? customSuccessUrl = null);
    }

    public class PaymongoService : IPaymongoService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _successUrl;
        private readonly string _cancelUrl;
        private readonly HttpClient _httpClient;

        public PaymongoService(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["Paymongo:SecretKey"];
            _successUrl = _configuration["Paymongo:SuccessUrl"];
            _cancelUrl = _configuration["Paymongo:CancelUrl"];
            
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.paymongo.com/v1/");

            if (!string.IsNullOrEmpty(_secretKey))
            {
                var authBytes = Encoding.ASCII.GetBytes($"{_secretKey}:");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            }
        }

        public async Task<string> CreatePaymentLinkAsync(Invoice invoice, string? customSuccessUrl = null)
        {
            var successUrl = !string.IsNullOrEmpty(customSuccessUrl) ? customSuccessUrl : _successUrl;

            if (string.IsNullOrEmpty(_secretKey) || _secretKey == "sk_test_YOUR_PAYMONGO_SECRET_KEY")
            {
                // Simulate success for development when key is not provided
                return successUrl;
            }

            var requestBody = new
            {
                data = new
                {
                    attributes = new
                    {
                        // Amount in centavos (e.g. 100000 = ₱1,000.00)
                        amount = (int)(invoice.Amount * 100),
                        description = $"Invoice {invoice.InvoiceNumber}",
                        remarks = $"Payment for Invoice {invoice.InvoiceNumber}",
                        payment_method_allowed = new[]
                        {
                            "gcash",
                            "paymaya",
                            "card",
                            "qrph",
                            "grab_pay",
                            "billease",
                            "dob",
                            "dob_ubp"
                        },
                        redirect = new
                        {
                            success = successUrl,
                            failed = _cancelUrl
                        }
                    }
                }
            };

            var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("links", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseContent);
                var checkoutUrl = jsonResponse["data"]["attributes"]["checkout_url"].ToString();
                return checkoutUrl;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Paymongo API request failed: {errorContent}");
            }
        }
    }
}
