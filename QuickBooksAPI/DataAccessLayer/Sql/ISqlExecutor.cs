namespace QuickBooksAPI.DataAccessLayer.Sql;

/// <summary>
/// Single-operation SQL execution using <see cref="ISqlConnectionFactory"/> and Dapper with consistent cancellation support.
/// </summary>
public interface ISqlExecutor
{
    Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);

    Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
}
