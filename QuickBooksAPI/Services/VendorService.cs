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

        public async Task<ApiResponse<PagedResult<Vendor>>> ListVendorsAsync(ListQueryParams query)
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<PagedResult<Vendor>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            var page = query.GetPage();
            var pageSize = query.GetPageSize();
            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
            var activeFilter = query.GetActiveFilter();
            var result = await _vendorRepository.GetPagedByUserAndRealmAsync(userId, realmId, page, pageSize, search, activeFilter);
            return ApiResponse<PagedResult<Vendor>>.Ok(result);
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

                var errors = CleanAndValidateCreateVendorRequest(request);
                if (errors.Count > 0)
                    return ApiResponse<string>.Fail("Validation failed.", errors.ToArray());

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

                var errors = CleanAndValidateUpdateVendorRequest(request);
                if (errors.Count > 0)
                    return ApiResponse<string>.Fail("Validation failed.", errors.ToArray());

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
        private List<string> CleanAndValidateCreateVendorRequest(CreateVendorRequest request)
        {
            request.DisplayName = request.DisplayName?.Trim() ?? string.Empty;
            request.GivenName = NormalizeString(request.GivenName);
            request.MiddleName = NormalizeString(request.MiddleName);
            request.FamilyName = NormalizeString(request.FamilyName);
            request.Title = NormalizeString(request.Title);
            request.Suffix = NormalizeString(request.Suffix);
            request.CompanyName = NormalizeString(request.CompanyName);
            request.PrintOnCheckName = NormalizeString(request.PrintOnCheckName);
            request.AcctNum = NormalizeString(request.AcctNum);
            request.TaxIdentifier = NormalizeString(request.TaxIdentifier);

            var cleanedEmail = CleanEmail(request.PrimaryEmailAddr?.Address);
            if (cleanedEmail == null) request.PrimaryEmailAddr = null;
            else if (request.PrimaryEmailAddr != null) request.PrimaryEmailAddr.Address = cleanedEmail;

            var cleanedPhone = NormalizeString(request.PrimaryPhone?.FreeFormNumber);
            if (cleanedPhone == null) request.PrimaryPhone = null;
            else if (request.PrimaryPhone != null) request.PrimaryPhone.FreeFormNumber = cleanedPhone;

            var cleanedMobile = NormalizeString(request.Mobile?.FreeFormNumber);
            if (cleanedMobile == null) request.Mobile = null;
            else if (request.Mobile != null) request.Mobile.FreeFormNumber = cleanedMobile;

            var cleanedWebUri = NormalizeString(request.WebAddr?.URI);
            if (cleanedWebUri == null) request.WebAddr = null;
            else if (request.WebAddr != null) request.WebAddr.URI = cleanedWebUri;

            if (request.BillAddr != null)
            {
                request.BillAddr.Line1 = NormalizeString(request.BillAddr.Line1);
                request.BillAddr.Line2 = NormalizeString(request.BillAddr.Line2);
                request.BillAddr.Line3 = NormalizeString(request.BillAddr.Line3);
                request.BillAddr.City = NormalizeString(request.BillAddr.City);
                request.BillAddr.CountrySubDivisionCode = NormalizeString(request.BillAddr.CountrySubDivisionCode);
                request.BillAddr.PostalCode = NormalizeString(request.BillAddr.PostalCode);
                request.BillAddr.Country = NormalizeString(request.BillAddr.Country);

                if (IsAddressEmpty(request.BillAddr.Line1, request.BillAddr.City,
                    request.BillAddr.CountrySubDivisionCode, request.BillAddr.PostalCode, request.BillAddr.Country))
                    request.BillAddr = null;
            }

            return ValidateVendorData(request.DisplayName, request.GivenName, request.FamilyName,
                request.Title, request.Suffix, cleanedEmail, cleanedPhone, request.BillAddr, isDisplayNameRequired: true);
        }

        private List<string> CleanAndValidateUpdateVendorRequest(UpdateVendorRequest request)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(request.Id)) errors.Add("Vendor ID is required.");
            if (string.IsNullOrWhiteSpace(request.SyncToken)) errors.Add("SyncToken is required.");

            request.DisplayName = NormalizeString(request.DisplayName);
            request.GivenName = NormalizeString(request.GivenName);
            request.MiddleName = NormalizeString(request.MiddleName);
            request.FamilyName = NormalizeString(request.FamilyName);
            request.Title = NormalizeString(request.Title);
            request.Suffix = NormalizeString(request.Suffix);
            request.CompanyName = NormalizeString(request.CompanyName);
            request.PrintOnCheckName = NormalizeString(request.PrintOnCheckName);
            request.AcctNum = NormalizeString(request.AcctNum);
            request.TaxIdentifier = NormalizeString(request.TaxIdentifier);

            var cleanedEmail = CleanEmail(request.PrimaryEmailAddr?.Address);
            if (cleanedEmail == null) request.PrimaryEmailAddr = null;
            else if (request.PrimaryEmailAddr != null) request.PrimaryEmailAddr.Address = cleanedEmail;

            var cleanedPhone = NormalizeString(request.PrimaryPhone?.FreeFormNumber);
            if (cleanedPhone == null) request.PrimaryPhone = null;
            else if (request.PrimaryPhone != null) request.PrimaryPhone.FreeFormNumber = cleanedPhone;

            var cleanedMobile = NormalizeString(request.Mobile?.FreeFormNumber);
            if (cleanedMobile == null) request.Mobile = null;
            else if (request.Mobile != null) request.Mobile.FreeFormNumber = cleanedMobile;

            var cleanedWebUri = NormalizeString(request.WebAddr?.URI);
            if (cleanedWebUri == null) request.WebAddr = null;
            else if (request.WebAddr != null) request.WebAddr.URI = cleanedWebUri;

            if (request.BillAddr != null)
            {
                request.BillAddr.Line1 = NormalizeString(request.BillAddr.Line1);
                request.BillAddr.Line2 = NormalizeString(request.BillAddr.Line2);
                request.BillAddr.Line3 = NormalizeString(request.BillAddr.Line3);
                request.BillAddr.City = NormalizeString(request.BillAddr.City);
                request.BillAddr.CountrySubDivisionCode = NormalizeString(request.BillAddr.CountrySubDivisionCode);
                request.BillAddr.PostalCode = NormalizeString(request.BillAddr.PostalCode);
                request.BillAddr.Country = NormalizeString(request.BillAddr.Country);

                if (request.BillAddr.Line1 == null && request.BillAddr.City == null &&
                    request.BillAddr.CountrySubDivisionCode == null && request.BillAddr.PostalCode == null &&
                    request.BillAddr.Country == null)
                    request.BillAddr = null;
            }

            errors.AddRange(ValidateVendorData(request.DisplayName, request.GivenName, request.FamilyName,
                request.Title, request.Suffix, cleanedEmail, cleanedPhone, null, isDisplayNameRequired: false));
            return errors;
        }

        private static List<string> ValidateVendorData(string? displayName, string? givenName, string? familyName,
            string? title, string? suffix, string? email, string? phone, VendorBillAddr? billAddr, bool isDisplayNameRequired)
        {
            var errors = new List<string>();

            if (isDisplayNameRequired && string.IsNullOrWhiteSpace(displayName))
                errors.Add("Display name is required.");
            else if (displayName?.Length > 500)
                errors.Add("Display name must be 500 characters or less.");

            if (givenName?.Length > 100) errors.Add("First name must be 100 characters or less.");
            if (familyName?.Length > 100) errors.Add("Last name must be 100 characters or less.");
            if (title?.Length > 50) errors.Add("Title must be 50 characters or less.");
            if (suffix?.Length > 50) errors.Add("Suffix must be 50 characters or less.");

            if (email != null)
            {
                if (!IsValidEmail(email)) errors.Add("Please enter a valid email address.");
                else if (email.Length > 100) errors.Add("Email address must be 100 characters or less.");
            }

            if (phone?.Length > 50) errors.Add("Phone number must be 50 characters or less.");

            if (billAddr != null)
            {
                if (billAddr.Line1?.Length > 500) errors.Add("Street address must be 500 characters or less.");
                if (billAddr.City?.Length > 255) errors.Add("City must be 255 characters or less.");
                if (billAddr.CountrySubDivisionCode?.Length > 255) errors.Add("State/Province must be 255 characters or less.");
                if (billAddr.PostalCode?.Length > 30) errors.Add("Postal code must be 30 characters or less.");
                if (billAddr.Country?.Length > 255) errors.Add("Country must be 255 characters or less.");
            }

            return errors;
        }

        private static string? NormalizeString(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? CleanEmail(string? email)
        {
            var normalized = email?.Trim()?.ToLowerInvariant();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static bool IsAddressEmpty(string? line1, string? city, string? state, string? postalCode, string? country)
            => line1 == null && city == null && state == null && postalCode == null && country == null;

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
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
                Title = dto.Title,
                GivenName = dto.GivenName,
                MiddleName = dto.MiddleName,
                FamilyName = dto.FamilyName,
                Suffix = dto.Suffix,
                DisplayName = dto.DisplayName,
                CompanyName = dto.CompanyName,
                PrintOnCheckName = dto.PrintOnCheckName,
                Active = dto.Active,
                Balance = dto.Balance,
                PrimaryEmailAddr = dto.PrimaryEmailAddr?.Address,
                PrimaryPhone = dto.PrimaryPhone?.FreeFormNumber,
                Mobile = dto.Mobile?.FreeFormNumber,
                WebAddr = dto.WebAddr?.URI,
                TaxIdentifier = dto.TaxIdentifier,
                AcctNum = dto.AcctNum,
                BillAddrLine1 = dto.BillAddr?.Line1,
                BillAddrLine2 = dto.BillAddr?.Line2,
                BillAddrLine3 = dto.BillAddr?.Line3,
                BillAddrCity = dto.BillAddr?.City,
                BillAddrPostalCode = dto.BillAddr?.PostalCode,
                BillAddrCountrySubDivisionCode = dto.BillAddr?.CountrySubDivisionCode,
                BillAddrCountry = dto.BillAddr?.Country,
                Domain = dto.Domain,
                Sparse = dto.Sparse,
                CreateTime = dto.MetaData?.CreateTime != null ? new DateTimeOffset(dto.MetaData.CreateTime.ToUniversalTime(), TimeSpan.Zero) : DateTimeOffset.UtcNow,
                LastUpdatedTime = dto.MetaData?.LastUpdatedTime != null ? new DateTimeOffset(dto.MetaData.LastUpdatedTime.ToUniversalTime(), TimeSpan.Zero) : DateTimeOffset.UtcNow
            };
        }
    }
}
