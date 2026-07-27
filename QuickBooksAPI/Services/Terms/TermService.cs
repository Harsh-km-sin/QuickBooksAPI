using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksService.Services;

namespace QuickBooksAPI.Services.Terms
{
    public class TermService : ITermService
    {
        private readonly ITermRepository _termRepository;
        private readonly IQuickBooksTermService _quickBooksTermService;
        private readonly IAuthService _authService;
        private readonly ILogger<TermService> _logger;

        public TermService(
            ITermRepository termRepository,
            IQuickBooksTermService quickBooksTermService,
            IAuthService authService,
            ILogger<TermService> logger)
        {
            _termRepository = termRepository ?? throw new ArgumentNullException(nameof(termRepository));
            _quickBooksTermService = quickBooksTermService ?? throw new ArgumentNullException(nameof(quickBooksTermService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<QBOTerm>> GetTermsAsync(string realmId, bool activeOnly = true)
        {
            return await _termRepository.GetAllByRealmAsync(realmId, activeOnly);
        }

        public async Task<ApiResponse<int>> SyncTermsAsync(int userId, string realmId)
        {
            try
            {
                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                    return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });

                var json = await _quickBooksTermService.GetTermsAsync(token.AccessToken, realmId);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var termList = new List<QBOTerm>();

                if (root.TryGetProperty("QueryResponse", out var queryResp) &&
                    queryResp.TryGetProperty("Term", out var termsArr) &&
                    termsArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in termsArr.EnumerateArray())
                    {
                        var qboId = element.TryGetProperty("Id", out var idProp) ? idProp.GetString() : null;
                        var name = element.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() : null;

                        if (string.IsNullOrEmpty(qboId) || string.IsNullOrEmpty(name)) continue;

                        var syncToken = element.TryGetProperty("SyncToken", out var stProp) ? stProp.GetString() : null;
                        var active = !element.TryGetProperty("Active", out var actProp) || actProp.GetBoolean();
                        var type = element.TryGetProperty("Type", out var typeProp) ? typeProp.GetString() : null;

                        decimal? discountPercent = element.TryGetProperty("DiscountPercent", out var dpProp) && dpProp.ValueKind == JsonValueKind.Number ? dpProp.GetDecimal() : null;
                        int? discountDays = element.TryGetProperty("DiscountDays", out var ddProp) && ddProp.ValueKind == JsonValueKind.Number ? ddProp.GetInt32() : null;
                        int? dueDays = element.TryGetProperty("DueDays", out var dueDProp) && dueDProp.ValueKind == JsonValueKind.Number ? dueDProp.GetInt32() : null;
                        int? dayOfMonthDue = element.TryGetProperty("DayOfMonthDue", out var domProp) && domProp.ValueKind == JsonValueKind.Number ? domProp.GetInt32() : null;
                        int? dueNextMonthDays = element.TryGetProperty("DueNextMonthDays", out var dnmProp) && dnmProp.ValueKind == JsonValueKind.Number ? dnmProp.GetInt32() : null;

                        DateTimeOffset? createTime = null;
                        DateTimeOffset? lastUpdatedTime = null;

                        if (element.TryGetProperty("MetaData", out var meta))
                        {
                            if (meta.TryGetProperty("CreateTime", out var ct) && DateTimeOffset.TryParse(ct.GetString(), out var parsedCt))
                                createTime = parsedCt;
                            if (meta.TryGetProperty("LastUpdatedTime", out var lut) && DateTimeOffset.TryParse(lut.GetString(), out var parsedLut))
                                lastUpdatedTime = parsedLut;
                        }

                        termList.Add(new QBOTerm
                        {
                            QBOTermId = qboId,
                            RealmId = realmId,
                            SyncToken = syncToken,
                            Name = name,
                            Active = active,
                            Type = type,
                            DiscountPercent = discountPercent,
                            DiscountDays = discountDays,
                            DueDays = dueDays,
                            DayOfMonthDue = dayOfMonthDue,
                            DueNextMonthDays = dueNextMonthDays,
                            CreateTime = createTime,
                            LastUpdatedTime = lastUpdatedTime,
                            RawJson = element.GetRawText()
                        });
                    }
                }

                if (termList.Count > 0)
                {
                    await _termRepository.UpsertTermsAsync(termList);
                }

                return ApiResponse<int>.Ok(termList.Count, $"Successfully synced {termList.Count} terms.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Term sync failed for user {UserId}", userId);
                return ApiResponse<int>.Fail("Failed to sync terms.", new[] { ex.Message });
            }
        }
    }
}
