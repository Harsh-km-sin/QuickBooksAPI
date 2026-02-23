using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
                _logger.LogError("QBO token exchange failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
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
                _logger.LogError("QBO token refresh failed. StatusCode={StatusCode}, Response={ResponseBody}", response.StatusCode, content);
                throw new HttpRequestException(
                    $"QBO token refresh failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
                );
            }

            _logger.LogDebug("QBO token refresh completed.");
            return content;
        }

        public async Task<bool> DisconnectQboAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("DisconnectQboAsync called with null or empty refresh token.");
                return false;
            }

            var clientId = _config["QuickBooks:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError("QuickBooks:ClientId configuration is missing.");
                return false;
            }

            var clientSecret = _config["QuickBooks:ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogError("QuickBooks:ClientSecret configuration is missing.");
                return false;
            }

            var revokeUrl = _config["QuickBooks:RevokeUrl"];
            if (string.IsNullOrWhiteSpace(revokeUrl))
                throw new InvalidOperationException("QuickBooks:RevokeUrl configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();

            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            var body = JsonSerializer.Serialize(new { token = refreshToken });
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(revokeUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("QBO token revoked successfully.");
                return true;
            }

            _logger.LogWarning("QBO token revoke failed. StatusCode={StatusCode}", response.StatusCode);
            return false;
        }

        public async Task<string> GetCompanyInfoAsync(string accessToken, string realmId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var baseUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            // RequestURL is https://sandbox-quickbooks.api.intuit.com/v3/company
            // CompanyInfo endpoint: /v3/company/{realmId}/companyinfo/{realmId}
            var requestUrl = $"{baseUrl.TrimEnd('/')}/{realmId}/companyinfo/{realmId}";

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO CompanyInfo request failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO CompanyInfo request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }

            _logger.LogDebug("QBO CompanyInfo request succeeded. RealmId={RealmId}", realmId);
            return content;
        }
    }
}
