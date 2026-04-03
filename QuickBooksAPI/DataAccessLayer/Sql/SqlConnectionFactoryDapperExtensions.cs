using Dapper;

namespace QuickBooksAPI.DataAccessLayer.Sql;

/// <summary>
/// Builds <see cref="CommandDefinition"/> with shared command timeout and cancellation for repository Dapper calls.
/// </summary>
public static class SqlConnectionFactoryDapperExtensions
{
    public static CommandDefinition CreateCommand(
        this ISqlConnectionFactory factory,
        string commandText,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return new CommandDefinition(
            commandText,
            parameters,
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);
    }
}
