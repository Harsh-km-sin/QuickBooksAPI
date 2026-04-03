using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using QuickBooksShared.Options;

namespace QuickBooksAPI.DataAccessLayer.Sql;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly DatabaseOptions _options;

    public SqlConnectionFactory(IOptions<DatabaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public int? CommandTimeoutSeconds =>
        _options.CommandTimeoutSeconds is > 0 ? _options.CommandTimeoutSeconds : null;

    public IDbConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_options.DefaultConnection))
            throw new InvalidOperationException(
                "Database connection string is not configured (ConnectionStrings:DefaultConnection).");

        return new SqlConnection(_options.DefaultConnection);
    }
}
