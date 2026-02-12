using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vendor = QuickBooksAPI.DataAccessLayer.Models.Vendor;

namespace QuickBooksAPI.Services
{
    public class VendorService : IVendorService
    {
        private readonly ICurrentUser _currentUser;
        private readonly ITokenRepository _tokenRepository;
        private readonly IQuickBooksVendorService _vendorService;
        private readonly IQboSyncStateRepository _qboSyncStateRepository;
        private readonly IAuthService _authService;
        private readonly IVendorRepository _vendorRepository;


        public VendorService(ICurrentUser currentUser, IQuickBooksVendorService vendorService, ITokenRepository tokenRepository, IQboSyncStateRepository qboSyncStateRepository, IAuthService authService, IVendorRepository vendorRepository)
        {
            _vendorService = vendorService;
            _currentUser = currentUser;
            _tokenRepository = tokenRepository;
            _authService = authService;
            _qboSyncStateRepository = qboSyncStateRepository;
            _vendorRepository = vendorRepository;
        }

        public async Task<ApiResponse<IEnumerable<Vendor>>> ListVendorsAsync()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<IEnumerable<Vendor>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            var vendors = await _vendorRepository.GetAllByUserAndRealmAsync(userId, realmId);
            return ApiResponse<IEnumerable<Vendor>>.Ok(vendors);
        }

        public async Task<ApiResponse<int>> GetVendorsAsync()
        {
            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            
            // Check and refresh token if expired
            var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
            if (token == null)
            {
                return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.", new[] { "Token not found or refresh failed" });
            }

            const int PageSize = 1000;
            int startPosition = 1;
            int totalSynced = 0;

            var lastUpdatedAfter = await _qboSyncStateRepository
                .GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Vendors.ToString());
            var isFirstSync = !lastUpdatedAfter.HasValue;

            // Ensure LastUpdatedAfter from DB is treated as UTC
            // Use the exact time from the previous sync - QuickBooks ">" query will naturally skip already-synced records
            if (lastUpdatedAfter.HasValue)
            {
                if (lastUpdatedAfter.Value.Kind != DateTimeKind.Utc)
                {
                    lastUpdatedAfter = DateTime.SpecifyKind(lastUpdatedAfter.Value, DateTimeKind.Utc);
                }
                // No buffer needed - use exact time, query uses ">" to skip already-synced records
            }

            DateTime? maxUpdatedTime = null; // Track max from actual synced records only

            while (true)
            {
                var json = await _vendorService.GetVendorsAsync(
                    token.AccessToken,
                    realmId,
                    startPosition,
                    PageSize,
                    lastUpdatedAfter
                );

                var qbo = JsonSerializer.Deserialize<QuickBooksVendorQueryResponse>(json);
                var vendors = qbo?.QueryResponse?.Vendor;

                if (vendors == null || vendors.Count == 0)
                    break;

                // Track max LastUpdatedTime from synced records
                // QuickBooks returns timestamps in Pacific Time (-08:00) as ISO 8601 strings
                // The JSON deserializer parses them, but we need to ensure proper UTC conversion
                foreach (var vendor in vendors)
                {
                    if (vendor.MetaData?.LastUpdatedTime != null)
                    {
                        var dtoLastUpdated = vendor.MetaData.LastUpdatedTime;
                        
                        // Convert to UTC properly
                        // If Kind is UTC, use as-is; otherwise convert (handles Unspecified/Local)
                        DateTime dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                            ? dtoLastUpdated
                            : dtoLastUpdated.ToUniversalTime();
                        
                        if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                            maxUpdatedTime = dtoLastUpdatedUtc;
                    }
                }

                var vendorModels = vendors.Select(v => MapToVendorFromQueryDto(v, userId, realmId)).ToList();
                await _vendorRepository.UpsertVendorsAsync(vendorModels, userId, realmId);

                totalSynced += vendors.Count;

                if (vendors.Count < PageSize)
                    break;

                startPosition += PageSize;
            }

            // Update sync state after successful sync
            if (totalSynced > 0 && maxUpdatedTime.HasValue)
            {
                // Ensure we don't store a time in the future (safeguard against timezone issues)
                var timeToStore = maxUpdatedTime.Value;
                var nowUtc = DateTime.UtcNow;
                if (timeToStore > nowUtc.AddSeconds(30))
                {
                    timeToStore = nowUtc;
                }
                
                // We synced records, use the max LastUpdatedTime from those records (already UTC)
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Vendors.ToString(),
                    timeToStore
                );
            }
            else if (isFirstSync)
            {
                // First sync with no records - mark that we checked
                await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                    userId,
                    realmId,
                    QboEntityType.Vendors.ToString(),
                    DateTime.UtcNow
                );
            }
            // If no records synced and not first sync, don't update sync state (keep previous value)

            return ApiResponse<int>.Ok(totalSynced, $"Successfully synced {totalSynced} vendors.");
        }

        public async Task<ApiResponse<string>> CreateVendorAsync(CreateVendorRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                // Check and refresh token if expired
                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                {
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });

                var createResponse = await _vendorService.CreateVendorAsync(token.AccessToken, realmId, jsonPayload);
                var createdResponse = JsonSerializer.Deserialize<QuickBooksVendorMutationResponse>(createResponse);

                if (createdResponse?.Vendor == null)
                    throw new Exception("Failed to create vendor in QBO or response is invalid.");

                var createdVendor = createdResponse.Vendor;
                var vendor = MapToVendorFromQueryDto(createdVendor, userId, realmId);
                await _vendorRepository.UpsertVendorsAsync(new List<Vendor> { vendor }, userId, realmId);
                return ApiResponse<string>.Ok(createResponse, "Vendor created successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to create vendor in QuickBooks.", new[] { ex.Message });
            }
        }

        public async Task<ApiResponse<string>> SoftDeleteVendorAsync(SoftDeleteVendorRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId!);
                var realmId = _currentUser.RealmId!;

                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                {
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }

                var deleteResponse = await _vendorService.SoftDeleteVendorAsync(token.AccessToken, realmId, request.Id, request.SyncToken);
                var deletedResponse = JsonSerializer.Deserialize<QuickBooksVendorMutationResponse>(deleteResponse);

                if (deletedResponse?.Vendor == null)
                {
                    return ApiResponse<string>.Fail("Failed to delete vendor in QuickBooks. The vendor may already be inactive or invalid Id/SyncToken.");
                }

                var deletedVendor = deletedResponse.Vendor;
                var vendor = MapToVendorFromQueryDto(deletedVendor, userId, realmId);
                await _vendorRepository.UpsertVendorsAsync(new List<Vendor> { vendor }, userId, realmId);

                return ApiResponse<string>.Ok(deleteResponse, "Vendor deleted successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to delete vendor in QuickBooks.", new[] { ex.Message });
            }
        }

        public async Task<ApiResponse<string>> UpdatevendorAsync(UpdateVendorRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                // Check and refresh token if expired
                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                {
                    return ApiResponse<string>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });

                var updateResponse = await _vendorService.UpdateVendorAsync(token.AccessToken, realmId, jsonPayload);
                var updatedResponse = JsonSerializer.Deserialize<QuickBooksVendorMutationResponse>(updateResponse);

                if(updatedResponse?.Vendor == null)
                    throw new Exception("Failed to update vendor in QBO or response is invalid.");

                var updatedVendor = updatedResponse.Vendor;
                var vendor = MapToVendorFromQueryDto(updatedVendor, userId, realmId);

                await _vendorRepository.UpsertVendorsAsync(new List<Vendor> { vendor }, userId, realmId);
                return ApiResponse<string>.Ok(updateResponse, "Vendor created successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to create vendor in QuickBooks.", new[] { ex.Message });
            }
        }
        private Vendor MapToVendorFromQueryDto(QuickBooksVendorQueryDto dto, int userId, string realmId)
        {
            return new Vendor
            {
                QboId = dto.Id,
                UserId = userId.ToString(),
                RealmId = realmId,
                SyncToken = dto.SyncToken,
                Title = null, // Not available in query DTO
                GivenName = dto.GivenName,
                MiddleName = null, // Not available in query DTO
                FamilyName = dto.FamilyName,
                DisplayName = dto.DisplayName,
                CompanyName = dto.CompanyName,
                Active = dto.Active,
                Balance = dto.Balance,
                Domain = dto.Domain,
                Sparse = dto.Sparse,
                PrimaryEmailAddr = dto.PrimaryEmailAddr?.Address,
                PrimaryPhone = dto.PrimaryPhone?.FreeFormNumber,
                BillAddrLine1 = dto.BillAddr?.Line1,
                BillAddrCity = dto.BillAddr?.City,
                BillAddrPostalCode = dto.BillAddr?.PostalCode,
                BillAddrCountrySubDivisionCode = dto.BillAddr?.CountrySubDivisionCode,
                CreateTime = dto.MetaData?.CreateTime ?? DateTime.Now,
                LastUpdatedTime = dto.MetaData?.LastUpdatedTime ?? DateTime.Now
            };
        }
    }
}
