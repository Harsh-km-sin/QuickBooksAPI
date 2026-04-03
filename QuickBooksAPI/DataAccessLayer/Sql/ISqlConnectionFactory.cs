using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Sql;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();

    /// <summary>
    /// When configured (positive), applied as Dapper command timeout (seconds).
    /// </summary>
    int? CommandTimeoutSeconds { get; }
}
