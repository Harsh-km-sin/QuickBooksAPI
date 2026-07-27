using System.Text.Json.Serialization;

namespace QuickBooksAPI.Application.Reports;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountingMethod
{
    Accrual,
    Cash
}
