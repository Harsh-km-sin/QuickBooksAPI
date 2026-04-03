using Dapper;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Sql;

public sealed class SqlExecutor : ISqlExecutor
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlExecutor(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(_connectionFactory.CreateCommand(sql, param, cancellationToken));
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<T?>(_connectionFactory.CreateCommand(sql, param, cancellationToken));
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T?>(_connectionFactory.CreateCommand(sql, param, cancellationToken));
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<T>(_connectionFactory.CreateCommand(sql, param, cancellationToken));
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<T?>(_connectionFactory.CreateCommand(sql, param, cancellationToken));
    }
}
