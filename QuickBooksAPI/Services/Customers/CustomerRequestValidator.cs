using QuickBooksAPI.API.DTOs.Request;
using System.Net.Mail;

namespace QuickBooksAPI.Services.Customers;

internal sealed record CustomerDataForValidation(
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
    bool IsDisplayNameRequired);

public static class CustomerRequestValidator
{
    public static List<string> CleanAndValidateForCreate(CreateCustomerRequest request)
    {
        request.GivenName = NormalizeString(request.GivenName);
        request.MiddleName = NormalizeString(request.MiddleName);
        request.FamilyName = NormalizeString(request.FamilyName);
        request.Title = NormalizeString(request.Title);
        request.Suffix = NormalizeString(request.Suffix);
        request.DisplayName = request.DisplayName?.Trim() ?? string.Empty;
        request.FullyQualifiedName = NormalizeString(request.FullyQualifiedName);
        request.CompanyName = NormalizeString(request.CompanyName);
        request.Notes = NormalizeString(request.Notes);

        var cleanedEmail = CleanEmail(request.PrimaryEmailAddr?.Address);
        if (cleanedEmail == null)
            request.PrimaryEmailAddr = null;
        else if (request.PrimaryEmailAddr != null)
            request.PrimaryEmailAddr.Address = cleanedEmail;

        var cleanedPhone = NormalizeString(request.PrimaryPhone?.FreeFormNumber);
        if (cleanedPhone == null)
            request.PrimaryPhone = null;
        else if (request.PrimaryPhone != null)
            request.PrimaryPhone.FreeFormNumber = cleanedPhone;

        if (request.BillAddr != null)
        {
            request.BillAddr.Line1 = NormalizeString(request.BillAddr.Line1);
            request.BillAddr.City = NormalizeString(request.BillAddr.City);
            request.BillAddr.CountrySubDivisionCode = NormalizeString(request.BillAddr.CountrySubDivisionCode);
            request.BillAddr.PostalCode = NormalizeString(request.BillAddr.PostalCode);
            request.BillAddr.Country = NormalizeString(request.BillAddr.Country);

            if (IsAddressEmpty(request.BillAddr.Line1, request.BillAddr.City,
                    request.BillAddr.CountrySubDivisionCode, request.BillAddr.PostalCode, request.BillAddr.Country))
                request.BillAddr = null;
        }

        var data = new CustomerDataForValidation(
            request.DisplayName, request.GivenName, request.MiddleName, request.FamilyName,
            request.Title, request.Suffix, cleanedEmail, cleanedPhone,
            request.BillAddr?.Line1, request.BillAddr?.City, request.BillAddr?.CountrySubDivisionCode,
            request.BillAddr?.PostalCode, request.BillAddr?.Country,
            IsDisplayNameRequired: true);

        return ValidateCustomerData(data);
    }

    public static List<string> CleanAndValidateForUpdate(UpdateCustomerRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Id))
            errors.Add("Customer ID is required.");
        if (string.IsNullOrWhiteSpace(request.SyncToken))
            errors.Add("SyncToken is required.");

        request.GivenName = NormalizeString(request.GivenName);
        request.MiddleName = NormalizeString(request.MiddleName);
        request.FamilyName = NormalizeString(request.FamilyName);
        request.DisplayName = NormalizeString(request.DisplayName);
        request.FullyQualifiedName = NormalizeString(request.FullyQualifiedName);
        request.CompanyName = NormalizeString(request.CompanyName);
        request.PrintOnCheckName = NormalizeString(request.PrintOnCheckName);
        request.PreferredDeliveryMethod = NormalizeString(request.PreferredDeliveryMethod);

        var cleanedEmail = CleanEmail(request.PrimaryEmailAddr?.Address);
        if (cleanedEmail == null)
            request.PrimaryEmailAddr = null;
        else if (request.PrimaryEmailAddr != null)
            request.PrimaryEmailAddr.Address = cleanedEmail;

        var cleanedPhone = NormalizeString(request.PrimaryPhone?.FreeFormNumber);
        if (cleanedPhone == null)
            request.PrimaryPhone = null;
        else if (request.PrimaryPhone != null)
            request.PrimaryPhone.FreeFormNumber = cleanedPhone;

        if (request.BillAddr != null)
        {
            request.BillAddr.Line1 = NormalizeString(request.BillAddr.Line1);
            request.BillAddr.City = NormalizeString(request.BillAddr.City);
            request.BillAddr.CountrySubDivisionCode = NormalizeString(request.BillAddr.CountrySubDivisionCode);
            request.BillAddr.PostalCode = NormalizeString(request.BillAddr.PostalCode);

            if (IsAddressEmpty(request.BillAddr.Line1, request.BillAddr.City,
                    request.BillAddr.CountrySubDivisionCode, request.BillAddr.PostalCode, null))
                request.BillAddr = null;
        }

        var data = new CustomerDataForValidation(
            request.DisplayName, request.GivenName, request.MiddleName, request.FamilyName,
            null, null, cleanedEmail, cleanedPhone,
            request.BillAddr?.Line1, request.BillAddr?.City, request.BillAddr?.CountrySubDivisionCode,
            request.BillAddr?.PostalCode, null,
            IsDisplayNameRequired: false);

        errors.AddRange(ValidateCustomerData(data));
        return errors;
    }

    private static List<string> ValidateCustomerData(CustomerDataForValidation data)
    {
        var errors = new List<string>();

        if (data.IsDisplayNameRequired && string.IsNullOrWhiteSpace(data.DisplayName))
            errors.Add("Display name is required.");
        else if (data.DisplayName?.Length > 500)
            errors.Add("Display name must be 500 characters or less.");

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

        if (data.Email != null)
        {
            if (!IsValidEmail(data.Email))
                errors.Add("Please enter a valid email address.");
            else if (data.Email.Length > 100)
                errors.Add("Email address must be 100 characters or less.");
        }

        if (data.Phone?.Length > 30)
            errors.Add("Phone number must be 30 characters or less.");

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

    private static string? NormalizeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }

    private static string? CleanEmail(string? email)
    {
        var normalized = email?.Trim()?.ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsAddressEmpty(string? line1, string? city, string? state, string? postalCode, string? country)
    {
        return line1 == null && city == null && state == null && postalCode == null && country == null;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
