using QuickBooksAPI.API.DTOs.Request;
using System.Net.Mail;

namespace QuickBooksAPI.Services.Vendors;

public static class VendorRequestValidator
{
    public static List<string> CleanAndValidateForCreate(CreateVendorRequest request)
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

    public static List<string> CleanAndValidateForUpdate(UpdateVendorRequest request)
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
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
