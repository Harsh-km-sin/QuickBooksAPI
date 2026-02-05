using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksAuthService : IQuickBooksAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<QuickBooksAuthService> _logger;

        public QuickBooksAuthService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<QuickBooksAuthService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> HandleCallbackAsync(string code, string realmId)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code cannot be null or empty.", nameof(code));
            
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var clientId = _config["QuickBooks:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("QuickBooks:ClientId configuration is missing or empty.");

            var clientSecret = _config["QuickBooks:ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("QuickBooks:ClientSecret configuration is missing or empty.");

            var redirectUri = _config["QuickBooks:RedirectUri"];
            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new InvalidOperationException("QuickBooks:RedirectUri configuration is missing or empty.");

            var tokenUrl = _config["QuickBooks:TokenUrl"];
            if (string.IsNullOrWhiteSpace(tokenUrl))
                throw new InvalidOperationException("QuickBooks:TokenUrl configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            
            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri }
            });

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QBO token exchange failed. StatusCode={StatusCode}, RealmId={RealmId}", response.StatusCode, realmId);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}."
                );
            }

            _logger.LogDebug("QBO token exchange completed. RealmId={RealmId}", realmId);
            return content;
        }

        public async Task<string> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token cannot be null or empty.", nameof(refreshToken));

            var clientId = _config["QuickBooks:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("QuickBooks:ClientId configuration is missing or empty.");

            var clientSecret = _config["QuickBooks:ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("QuickBooks:ClientSecret configuration is missing or empty.");

            var tokenUrl = _config["QuickBooks:TokenUrl"];
            if (string.IsNullOrWhiteSpace(tokenUrl))
                throw new InvalidOperationException("QuickBooks:TokenUrl configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            
            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            });

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QBO token refresh failed. StatusCode={StatusCode}", response.StatusCode);
                throw new HttpRequestException(
                    $"QBO token refresh failed. Status={(int)response.StatusCode} {response.ReasonPhrase}."
                );
            }

            _logger.LogDebug("QBO token refresh completed.");
            return content;
        }
    }
}
