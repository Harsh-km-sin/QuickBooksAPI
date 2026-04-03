namespace QuickBooksShared.Options;

public class DatabaseOptions
{
    public string DefaultConnection { get; set; } = string.Empty;

    /// <summary>
    /// When set to a positive value, the SQL data-access executor passes it to Dapper command timeout (seconds).
    /// Repository code that calls Dapper directly should honor the same option via <c>CommandDefinition</c>.
    /// When null or omitted, the provider default is used.
    /// </summary>
    public int? CommandTimeoutSeconds { get; set; }
}

