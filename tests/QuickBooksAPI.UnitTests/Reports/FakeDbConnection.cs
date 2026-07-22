using System.Data;

namespace QuickBooksAPI.UnitTests.Reports;

/// <summary>
/// Minimal in-memory <see cref="IDbConnection"/> for tests that exercise the sync service's
/// transaction handling without touching a database. The repository itself is mocked, so nothing
/// here needs to execute SQL — it only has to let BeginTransaction/Commit/Dispose succeed.
/// </summary>
internal sealed class FakeDbConnection : IDbConnection
{
    public string ConnectionString { get; set; } = string.Empty;
    public int ConnectionTimeout => 0;
    public string Database => string.Empty;
    public ConnectionState State { get; private set; } = ConnectionState.Open;

    public IDbTransaction BeginTransaction() => new FakeDbTransaction(this);

    public IDbTransaction BeginTransaction(IsolationLevel il) => new FakeDbTransaction(this);

    public void ChangeDatabase(string databaseName) { }

    public void Close() => State = ConnectionState.Closed;

    public IDbCommand CreateCommand() => throw new NotSupportedException(
        "FakeDbConnection does not execute commands; mock the repository instead.");

    public void Open() => State = ConnectionState.Open;

    public void Dispose() => State = ConnectionState.Closed;

    private sealed class FakeDbTransaction : IDbTransaction
    {
        public FakeDbTransaction(IDbConnection connection) => Connection = connection;

        public IDbConnection? Connection { get; }
        public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }

        public void Commit() => Committed = true;
        public void Rollback() => RolledBack = true;
        public void Dispose() { }
    }
}
