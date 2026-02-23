using Azure.Core;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksService.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace QuickBooksAPI.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICurrentUser _currentUser;
        private readonly ITokenRepository _tokenRepository;
        private readonly IQuickBooksCustomerService _quickBooksCustomerService;
        private readonly ICustomerRepository _customerRepository;
        private readonly IQboSyncStateRepository _qboSyncStateRepository;
        private readonly IAuthService _authService;
        private readonly ILogger<CustomerService> _logger;
        public CustomerService(
            ICurrentUser currentUser,
            ITokenRepository tokenRepository, 
            IQuickBooksCustomerService quickBooksCustomerService, 
            ICustomerRepository customerRepository,
            IQboSyncStateRepository qboSyncStateRepository,
            IAuthService authService,
            ILogger<CustomerService> logger) 
        {
            _currentUser = currentUser;
            _quickBooksCustomerService = quickBooksCustomerService;
            _tokenRepository = tokenRepository;
            _customerRepository = customerRepository;
            _qboSyncStateRepository = qboSyncStateRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<Customer>>> ListCustomersAsync()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<IEnumerable<Customer>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            var customers = await _customerRepository.GetAllByUserAndRealmAsync(userId, realmId);
            return ApiResponse<IEnumerable<Customer>>.Ok(customers);
        }

        public async Task<ApiResponse<PagedResult<Customer>>> ListCustomersAsync(ListQueryParams query)
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<PagedResult<Customer>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            var page = query.GetPage();
            var pageSize = query.GetPageSize();
            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
            var activeFilter = query.GetActiveFilter();
            var result = await _customerRepository.GetPagedByUserAndRealmAsync(userId, realmId, page, pageSize, search, activeFilter);
            return ApiResponse<PagedResult<Customer>>.Ok(result);
        }

        public async Task<ApiResponse<Customer>> GetCustomerByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(_currentUser.UserId) || string.IsNullOrEmpty(_currentUser.RealmId))
                return ApiResponse<Customer>.Fail("User context is missing. Please sign in and connect QuickBooks.");

            if (string.IsNullOrWhiteSpace(id))
                return ApiResponse<Customer>.Fail("Customer id is required.");

            var userId = int.Parse(_currentUser.UserId);
            var realmId = _currentUser.RealmId;
            var customer = await _customerRepository.GetByQboIdAsync(userId, realmId, id.Trim());
            if (customer == null)
                return ApiResponse<Customer>.Fail("Customer not found.");

            return ApiResponse<Customer>.Ok(customer);
        }

        public async Task<ApiResponse<int>> GetCustomersAsync()
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                // Check and refresh token if expired
                var token = await _authService.RefreshTokenIfExpiredAsync(userId, realmId);
                if (token == null)
                {
                    return ApiResponse<int>.Fail("No valid access token found. Please reconnect QuickBooks.");
                }

                var lastUpdatedAfter = await _qboSyncStateRepository.GetLastUpdatedAfterAsync(userId, realmId, QboEntityType.Customer.ToString());
                _logger.LogInformation($"[CustomerSync] Started sync for user {userId}. LastUpdatedAfter from DB: {lastUpdatedAfter}");

                var isFirstSync = !lastUpdatedAfter.HasValue;
                var originalLastUpdatedAfter = lastUpdatedAfter; // Store original for fallback

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
                    
                const int PageSize = 1000;
                int startPosition = 1;
                int totalSynced = 0;
                bool hasMore = true;
                DateTime? maxUpdatedTime = null; // Track max from actual synced records only

                while (hasMore)
                {
                    var customersJson = await _quickBooksCustomerService.GetCustomersAsync(token.AccessToken, realmId, startPosition, PageSize, lastUpdatedAfter);
                    var customerResponse = JsonSerializer.Deserialize<QuickBooksCustomerQueryResponse>(customersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var customerCount = customerResponse?.QueryResponse?.Customers?.Count ?? 0;
                    _logger.LogInformation($"[CustomerSync] Fetched page starting at {startPosition}. Count: {customerCount}");

                    var customers = customerResponse?.QueryResponse?.Customers?.Select(c => MapDtoToCustomer(c, userId, realmId)).ToList();

                    if (customers != null)
                    {
                        foreach (var c in customers)
                        {
                            _logger.LogInformation("[CustomerSync] Processing customer. QboId={QboId}, LastUpdatedTime={LastUpdatedTime:O}", c.QboId, c.LastUpdatedTime);
                        }
                    }

                    if (customers == null || customers.Count == 0)
                    {
                        hasMore = false;
                        continue;
                    }

                    // Track max LastUpdatedTime from synced records
                    // QuickBooks returns timestamps in Pacific Time (-08:00) as ISO 8601 strings
                    // The JSON deserializer parses them, but we need to ensure proper UTC conversion
                    foreach (var dto in customerResponse.QueryResponse.Customers)
                    {
                        if (dto.MetaData?.LastUpdatedTime != default)
                        {
                            var dtoLastUpdated = dto.MetaData.LastUpdatedTime;
                            
                            // Convert to UTC properly
                            // If Kind is UTC, use as-is; otherwise convert (handles Unspecified/Local)
                            DateTime dtoLastUpdatedUtc = dtoLastUpdated.Kind == DateTimeKind.Utc
                                ? dtoLastUpdated
                                : dtoLastUpdated.ToUniversalTime();
                            
                            if (!maxUpdatedTime.HasValue || dtoLastUpdatedUtc > maxUpdatedTime.Value)
                            {
                                maxUpdatedTime = dtoLastUpdatedUtc;
                                _logger.LogInformation($"[CustomerSync] Tracking MaxLastUpdatedTime from DTO: {dtoLastUpdatedUtc:O} (original: {dtoLastUpdated:O}, Kind: {dtoLastUpdated.Kind})");
                            }
                        }
                    }

                    // Upsert customers (stored procedure may return -1 if SET NOCOUNT ON, so count actual records)
                    await _customerRepository.UpsertCustomersAsync(customers, userId, realmId);
                    totalSynced += customers.Count;

                    if (customers.Count < PageSize)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        startPosition += PageSize;
                    }
                }

                // Update sync state after successful sync
                // Always use maxUpdatedTime from synced records (MetaData.LastUpdatedTime), never current time
                if (totalSynced > 0)
                {
                    DateTime timeToStore;
                    
                    if (maxUpdatedTime.HasValue)
                    {
                        // Use the exact maximum LastUpdatedTime from the records we actually received
                        // Store it as-is - the query uses ">" (not ">=") so it will skip already-synced records
                        timeToStore = maxUpdatedTime.Value;
                        
                        // Ensure we don't store a time in the future (safeguard against timezone issues)
                        var nowUtc = DateTime.UtcNow;
                        if (timeToStore > nowUtc.AddSeconds(30))
                        {
                            timeToStore = nowUtc;
                        }
                    }
                    else
                    {
                        // Fallback: if records were synced but none had LastUpdatedTime (shouldn't happen with QBO)
                        // Use the original lastUpdatedAfter (before buffer) as a safe fallback
                        if (originalLastUpdatedAfter.HasValue)
                        {
                            timeToStore = originalLastUpdatedAfter.Value;
                        }
                        else
                        {
                            // Last resort: use current time only if this is truly unexpected
                            timeToStore = DateTime.UtcNow;
                        }
                    }

                    // Store the exact max LastUpdatedTime from records (already UTC)
                    // Query uses ">" (not ">=") so it will skip already-synced records
                    await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                        userId,
                        realmId,
                        QboEntityType.Customer.ToString(),
                        timeToStore
                    );
                    _logger.LogInformation($"[CustomerSync] Updated SyncState with MaxLastUpdatedTime from records: {timeToStore:O} (max from records: {maxUpdatedTime:O})");
                }
                else if (isFirstSync)
                {
                    // First sync with no records - mark that we checked
                    await _qboSyncStateRepository.UpdateLastUpdatedAfterAsync(
                        userId,
                        realmId,
                        QboEntityType.Customer.ToString(),
                        DateTime.UtcNow
                    );
                }
                // If no records synced and not first sync, don't update sync state (keep previous value)
                // This is correct behavior: if nothing changed in QuickBooks, we keep the same cursor
                // so the next sync will query from the same point and won't miss any records
                if (totalSynced == 0 && !isFirstSync)
                {
                    _logger.LogInformation($"[CustomerSync] Completed with 0 customers synced. SyncState remains unchanged (LastUpdatedAfter: {originalLastUpdatedAfter:O}).");
                }

                return ApiResponse<int>.Ok(totalSynced, $"Successfully synced {totalSynced} customers.");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail("Failed to sync customers.", new[] { ex.Message });
            }
        }
        public async Task<ApiResponse<string>> CreateCustomerAsync(CreateCustomerRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;
                
                // Clean and validate the request data
                var validationErrors = CleanAndValidateCustomerRequest(request);
                if (validationErrors.Count > 0)
                {
                    return ApiResponse<string>.Fail("Validation failed.", validationErrors);
                }

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

                var createResponse = await _quickBooksCustomerService.CreateCustomerAsync(token.AccessToken, realmId, jsonPayload);

                var createdResponse = JsonSerializer.Deserialize<QuickBooksCustomerMutationResponse>(createResponse);

                if (createdResponse?.Customer == null)
                    throw new Exception("Failed to create customer in QBO or response is invalid.");

                var createdCustomer = createdResponse.Customer;
                var customer = MapDtoToCustomer(createdCustomer, userId, realmId);

                await _customerRepository.UpsertCustomersAsync(new List<Customer> { customer }, userId, realmId);
                return ApiResponse<string>.Ok(createResponse, "Customer created successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to create customer in QuickBooks.", new[] { ex.Message });
            }
        }

        #region Customer Data Cleanup and Validation

        /// <summary>
        /// Internal record for normalized customer data used by validation.
        /// </summary>
        private record CustomerDataForValidation(
            string? DisplayName,
            string? GivenName,
            string? MiddleName,
            string? FamilyName,
            string? Title,
            string? Suffix,
            string? Email,
            string? Phone,
            string? AddressLine1,
            string? City,
            string? State,
            string? PostalCode,
            string? Country,
            bool IsDisplayNameRequired
        );

        /// <summary>
        /// Cleans and validates the CreateCustomerRequest.
        /// </summary>
        private List<string> CleanAndValidateCustomerRequest(CreateCustomerRequest request)
        {
            // Normalize all string fields
            request.GivenName = NormalizeString(request.GivenName);
            request.MiddleName = NormalizeString(request.MiddleName);
            request.FamilyName = NormalizeString(request.FamilyName);
            request.Title = NormalizeString(request.Title);
            request.Suffix = NormalizeString(request.Suffix);
            request.DisplayName = request.DisplayName?.Trim() ?? string.Empty;
            request.FullyQualifiedName = NormalizeString(request.FullyQualifiedName);
            request.CompanyName = NormalizeString(request.CompanyName);
            request.Notes = NormalizeString(request.Notes);

            // Clean email
            var cleanedEmail = CleanEmail(request.PrimaryEmailAddr?.Address);
            if (cleanedEmail == null)
                request.PrimaryEmailAddr = null;
            else if (request.PrimaryEmailAddr != null)
                request.PrimaryEmailAddr.Address = cleanedEmail;

            // Clean phone
            var cleanedPhone = NormalizeString(request.PrimaryPhone?.FreeFormNumber);
            if (cleanedPhone == null)
                request.PrimaryPhone = null;
            else if (request.PrimaryPhone != null)
                request.PrimaryPhone.FreeFormNumber = cleanedPhone;

            // Clean address
            if (request.BillAddr != null)
            {
                request.BillAddr.Line1 = NormalizeString(request.BillAddr.Line1);
                request.BillAddr.City = NormalizeString(request.BillAddr.City);
                request.BillAddr.CountrySubDivisionCode = NormalizeString(request.BillAddr.CountrySubDivisionCode);
                request.BillAddr.PostalCode = NormalizeString(request.BillAddr.PostalCode);
                request.BillAddr.Country = NormalizeString(request.BillAddr.Country);

                if (IsAddressEmpty(request.BillAddr.Line1, request.BillAddr.City, 
                    request.BillAddr.CountrySubDivisionCode, request.BillAddr.PostalCode, request.BillAddr.Country))
                {
                    request.BillAddr = null;
                }
            }

            // Build validation data and validate
            var data = new CustomerDataForValidation(
                request.DisplayName, request.GivenName, request.MiddleName, request.FamilyName,
                request.Title, request.Suffix, cleanedEmail, cleanedPhone,
                request.BillAddr?.Line1, request.BillAddr?.City, request.BillAddr?.CountrySubDivisionCode,
                request.BillAddr?.PostalCode, request.BillAddr?.Country,
                IsDisplayNameRequired: true
            );

            return ValidateCustomerData(data);
        }

        /// <summary>
        /// Cleans and validates the UpdateCustomerRequest.
        /// </summary>
        private List<string> CleanAndValidateUpdateCustomerRequest(UpdateCustomerRequest request)
        {
            var errors = new List<string>();

            // Validate update-specific required fields
            if (string.IsNullOrWhiteSpace(request.Id))
                errors.Add("Customer ID is required.");
            if (string.IsNullOrWhiteSpace(request.SyncToken))
                errors.Add("SyncToken is required.");

            // Normalize all string fields
            request.GivenName = NormalizeString(request.GivenName);
            request.MiddleName = NormalizeString(request.MiddleName);
            request.FamilyName = NormalizeString(request.FamilyName);
            request.DisplayName = NormalizeString(request.DisplayName);
            request.FullyQualifiedName = NormalizeString(request.FullyQualifiedName);
            request.CompanyName = NormalizeString(request.CompanyName);
            request.PrintOnCheckName = NormalizeString(request.PrintOnCheckName);
            request.PreferredDeliveryMethod = NormalizeString(request.PreferredDeliveryMethod);

            // Clean email
            var cleanedEmail = CleanEmail(request.PrimaryEmailAddr?.Address);
            if (cleanedEmail == null)
                request.PrimaryEmailAddr = null;
            else if (request.PrimaryEmailAddr != null)
                request.PrimaryEmailAddr.Address = cleanedEmail;

            // Clean phone
            var cleanedPhone = NormalizeString(request.PrimaryPhone?.FreeFormNumber);
            if (cleanedPhone == null)
                request.PrimaryPhone = null;
            else if (request.PrimaryPhone != null)
                request.PrimaryPhone.FreeFormNumber = cleanedPhone;

            // Clean address
            if (request.BillAddr != null)
            {
                request.BillAddr.Line1 = NormalizeString(request.BillAddr.Line1);
                request.BillAddr.City = NormalizeString(request.BillAddr.City);
                request.BillAddr.CountrySubDivisionCode = NormalizeString(request.BillAddr.CountrySubDivisionCode);
                request.BillAddr.PostalCode = NormalizeString(request.BillAddr.PostalCode);

                if (IsAddressEmpty(request.BillAddr.Line1, request.BillAddr.City,
                    request.BillAddr.CountrySubDivisionCode, request.BillAddr.PostalCode, null))
                {
                    request.BillAddr = null;
                }
            }

            // Build validation data and validate (DisplayName not required for updates)
            var data = new CustomerDataForValidation(
                request.DisplayName, request.GivenName, request.MiddleName, request.FamilyName,
                null, null, cleanedEmail, cleanedPhone,
                request.BillAddr?.Line1, request.BillAddr?.City, request.BillAddr?.CountrySubDivisionCode,
                request.BillAddr?.PostalCode, null,
                IsDisplayNameRequired: false
            );

            errors.AddRange(ValidateCustomerData(data));
            return errors;
        }

        /// <summary>
        /// Shared validation logic for customer data.
        /// </summary>
        private static List<string> ValidateCustomerData(CustomerDataForValidation data)
        {
            var errors = new List<string>();

            // DisplayName validation
            if (data.IsDisplayNameRequired && string.IsNullOrWhiteSpace(data.DisplayName))
                errors.Add("Display name is required.");
            else if (data.DisplayName?.Length > 500)
                errors.Add("Display name must be 500 characters or less.");

            // Name fields validation
            if (data.GivenName?.Length > 100)
                errors.Add("First name must be 100 characters or less.");
            if (data.FamilyName?.Length > 100)
                errors.Add("Last name must be 100 characters or less.");
            if (data.MiddleName?.Length > 100)
                errors.Add("Middle name must be 100 characters or less.");
            if (data.Title?.Length > 16)
                errors.Add("Title must be 16 characters or less.");
            if (data.Suffix?.Length > 16)
                errors.Add("Suffix must be 16 characters or less.");

            // Email validation
            if (data.Email != null)
            {
                if (!IsValidEmail(data.Email))
                    errors.Add("Please enter a valid email address.");
                else if (data.Email.Length > 100)
                    errors.Add("Email address must be 100 characters or less.");
            }

            // Phone validation
            if (data.Phone?.Length > 30)
                errors.Add("Phone number must be 30 characters or less.");

            // Address validation
            if (data.AddressLine1?.Length > 500)
                errors.Add("Street address must be 500 characters or less.");
            if (data.City?.Length > 255)
                errors.Add("City must be 255 characters or less.");
            if (data.State?.Length > 255)
                errors.Add("State/Province must be 255 characters or less.");
            if (data.PostalCode?.Length > 30)
                errors.Add("Postal code must be 30 characters or less.");
            if (data.Country?.Length > 255)
                errors.Add("Country must be 255 characters or less.");

            return errors;
        }

        /// <summary>
        /// Trims whitespace and converts empty/whitespace strings to null.
        /// </summary>
        private static string? NormalizeString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }

        /// <summary>
        /// Cleans email: trims, lowercases, returns null if empty.
        /// </summary>
        private static string? CleanEmail(string? email)
        {
            var normalized = email?.Trim()?.ToLowerInvariant();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        /// <summary>
        /// Checks if all address fields are empty.
        /// </summary>
        private static bool IsAddressEmpty(string? line1, string? city, string? state, string? postalCode, string? country)
        {
            return line1 == null && city == null && state == null && postalCode == null && country == null;
        }

        /// <summary>
        /// Validates email format.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

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

        #endregion

        public async Task<ApiResponse<string>> UpdateCustomerAsync(UpdateCustomerRequest request)
        {
            try
            {
                var userId = int.Parse(_currentUser.UserId);
                var realmId = _currentUser.RealmId;

                // Clean and validate the request data
                var validationErrors = CleanAndValidateUpdateCustomerRequest(request);
                if (validationErrors.Count > 0)
                {
                    return ApiResponse<string>.Fail("Validation failed.", validationErrors);
                }
                
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

                var updateResponse = await _quickBooksCustomerService.UpdateCustomerAsync(token.AccessToken, realmId, jsonPayload);
                var updatedResponse = JsonSerializer.Deserialize<QuickBooksCustomerMutationResponse>(updateResponse);
                if (updatedResponse?.Customer == null)
                    throw new Exception("Failed to update customer in QBO or response is invalid.");

                var updatedCustomer = updatedResponse.Customer;
                var customer = MapDtoToCustomer(updatedCustomer, userId, realmId);
                await _customerRepository.UpsertCustomersAsync(new List<Customer> { customer }, userId, realmId);
                return ApiResponse<string>.Ok(updateResponse, "Customer updated successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to update customer in QuickBooks.", new[] { ex.Message });
            }
        }
        public async Task<ApiResponse<string>> DeleteCustomerAsync(DeleteCustomerRequest request)
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
                var deleteResponse = await _quickBooksCustomerService.DeleteCustomerAsync(token.AccessToken, realmId, jsonPayload);
                var deletedResponse = JsonSerializer.Deserialize<QuickBooksCustomerMutationResponse>(deleteResponse);
                if (deletedResponse?.Customer == null)
                    throw new Exception("Failed to delete customer in QBO or response is invalid.");
                var deletedCustomer = deletedResponse.Customer;
                var customer = MapDtoToCustomer(deletedCustomer, userId, realmId);
                await _customerRepository.UpsertCustomersAsync(new List<Customer> { customer }, userId, realmId);
                return ApiResponse<string>.Ok(deleteResponse, "Customer deleted successfully in QuickBooks.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to delete customer in QuickBooks.", new[] { ex.Message });
            }
        }
        private Customer MapDtoToCustomer(QuickBooksCustomerDto dto, int userId, string realmId)
        {
            return new Customer
            {
                QboId = dto.QBOId,
                UserId = userId.ToString(), 
                RealmId = realmId,
                SyncToken = dto.SyncToken,

                Title = dto.Title,
                GivenName = dto.GivenName,
                MiddleName = dto.MiddleName,
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
