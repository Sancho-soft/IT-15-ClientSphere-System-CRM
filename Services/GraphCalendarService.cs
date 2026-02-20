using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using ClientSphere.Models;
using Microsoft.Extensions.Configuration;
using Azure.Core;
using Azure.Identity;

namespace ClientSphere.Services
{
    public class GraphCalendarService : ICalendarService
    {
        private readonly IConfiguration _configuration;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly string _redirectUri;

        public GraphCalendarService(IConfiguration configuration)
        {
            _configuration = configuration;
            _clientId = _configuration["MicrosoftGraph:ClientId"];
            _clientSecret = _configuration["MicrosoftGraph:ClientSecret"];
            _tenantId = _configuration["MicrosoftGraph:TenantId"] ?? "common";
            _redirectUri = _configuration["MicrosoftGraph:RedirectUri"];
        }

        public async Task<string> GetAuthorizationUrlAsync(string userId)
        {
            var scopes = new[] { "Calendars.ReadWrite", "User.Read" };
            var app = ConfidentialClientApplicationBuilder
                .Create(_clientId)
                .WithClientSecret(_clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_tenantId}"))
                .WithRedirectUri(_redirectUri)
                .Build();

            var authUrl = await app.GetAuthorizationRequestUrl(scopes)
                .ExecuteAsync();

            return authUrl.ToString();
        }

        public async Task<string> HandleCallbackAsync(string code, string userId)
        {
            var scopes = new[] { "Calendars.ReadWrite", "User.Read" };
            var app = ConfidentialClientApplicationBuilder
                .Create(_clientId)
                .WithClientSecret(_clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_tenantId}"))
                .WithRedirectUri(_redirectUri)
                .Build();

            var result = await app.AcquireTokenByAuthorizationCode(scopes, code).ExecuteAsync();
            return result.AccessToken;
        }

        public async Task<bool> SyncAppointmentAsync(Appointment appointment, string accessToken)
        {
            try
            {
                // Create a simple token credential
                var tokenCredential = new AccessTokenCredential(accessToken);
                var graphClient = new GraphServiceClient(tokenCredential);

                var newEvent = new Event
                {
                    Subject = appointment.Title,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = appointment.Description ?? ""
                    },
                    Start = new DateTimeTimeZone
                    {
                        DateTime = appointment.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        TimeZone = "UTC"
                    },
                    End = new DateTimeTimeZone
                    {
                        DateTime = appointment.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        TimeZone = "UTC"
                    },
                    Location = new Location
                    {
                        DisplayName = appointment.Location ?? "TBD"
                    }
                };

                var createdEvent = await graphClient.Me.Events.PostAsync(newEvent);
                
                // Store the Graph event ID for future updates/deletions
                appointment.ExternalCalendarId = createdEvent?.Id;
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAppointmentAsync(string graphEventId, string accessToken)
        {
            try
            {
                var tokenCredential = new AccessTokenCredential(accessToken);
                var graphClient = new GraphServiceClient(tokenCredential);

                await graphClient.Me.Events[graphEventId].DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Simple token credential implementation
        private class AccessTokenCredential : TokenCredential
        {
            private readonly string _accessToken;

            public AccessTokenCredential(string accessToken)
            {
                _accessToken = accessToken;
            }

            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                return new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1));
            }

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                return new ValueTask<AccessToken>(new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1)));
            }
        }
    }
}
