using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ClientSphere.Models;

namespace ClientSphere.Services
{
    public class PwnedPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : class
    {
        private readonly HttpClient _httpClient;

        public PwnedPasswordValidator(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ClientSphere-CRM");
        }

        public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return IdentityResult.Success;
            }

            var hash = ComputeSha1Hash(password);
            var prefix = hash.Substring(0, 5);
            var suffix = hash.Substring(5);

            try
            {
                var response = await _httpClient.GetStringAsync($"https://api.pwnedpasswords.com/range/{prefix}");
                var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2 && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        var count = int.Parse(parts[1]);
                        return IdentityResult.Failed(new IdentityError
                        {
                            Code = "PasswordPwned",
                            Description = $"This password has been exposed in a data breach {count:N0} times globally. For your security, please choose a different password."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // If API is down or times out, we fail open (allow registration) rather than breaking the application.
                Console.WriteLine($"[PwnedValidator] Error querying API: {ex.Message}");
                return IdentityResult.Success;
            }

            return IdentityResult.Success;
        }

        private static string ComputeSha1Hash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA1.HashData(bytes);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString(); // Returns upper case automatically via X2
        }
    }
}
