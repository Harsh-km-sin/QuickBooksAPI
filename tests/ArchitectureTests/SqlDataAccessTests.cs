using Dapper;
using Microsoft.Extensions.Options;
using QuickBooksAPI.DataAccessLayer.Sql;
using QuickBooksShared.Options;

namespace ArchitectureTests;

public class SqlDataAccessTests
{
    [Fact]
    public void SqlConnectionFactory_CreateConnection_WhenDefaultConnectionEmpty_Throws()
    {
        var factory = new SqlConnectionFactory(Options.Create(new DatabaseOptions { DefaultConnection = "" }));

        Assert.Throws<InvalidOperationException>(() => factory.CreateConnection());
    }

    [Fact]
    public void SqlConnectionFactory_CreateConnection_ReturnsOpenableConnection()
    {
        var factory = new SqlConnectionFactory(Options.Create(new DatabaseOptions
        {
            DefaultConnection = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;"
        }));

        using var conn = factory.CreateConnection();
        Assert.NotNull(conn);
        Assert.Equal("Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;", conn.ConnectionString);
    }

    [Fact]
    public void SqlConnectionFactory_CommandTimeoutSeconds_WhenConfigured_IsExposed()
    {
        var factory = new SqlConnectionFactory(Options.Create(new DatabaseOptions
        {
            DefaultConnection = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;",
            CommandTimeoutSeconds = 88
        }));

        Assert.Equal(88, factory.CommandTimeoutSeconds);
    }

    [Fact]
    public void CreateCommand_IncludesFactoryTimeoutAndCancellation()
    {
        using var cts = new CancellationTokenSource();
        var factory = new SqlConnectionFactory(Options.Create(new DatabaseOptions
        {
            DefaultConnection = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;",
            CommandTimeoutSeconds = 77
        }));

        CommandDefinition cmd = factory.CreateCommand("SELECT 1", new { }, cts.Token);

        Assert.Equal(77, cmd.CommandTimeout);
        Assert.Equal(cts.Token, cmd.CancellationToken);
    }
}
