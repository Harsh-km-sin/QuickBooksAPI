# SQL conventions (Phase 2)

## Inline SQL

- Prefer **parameterized** Dapper calls only (`@Param`); no string-concatenated predicates.
- If a single method’s SQL exceeds **~40 lines** or is duplicated, extract to:
  - a `private const string` or `static readonly` field on the repository, or
  - a dedicated `*Queries` static class, or
  - an embedded `.sql` file (build action: embedded resource) when the team standardizes on that approach.

## Transactions and batches

- Use `IDbConnection` / `IDbTransaction` from `ISqlConnectionFactory` when multiple statements must commit together.
- Do not open global static connections.

## Timeouts and cancellation

- Default command timeout: set `ConnectionStrings:CommandTimeoutSeconds` in configuration (bound to `DatabaseOptions`). Exposed on `ISqlConnectionFactory.CommandTimeoutSeconds`.
- Prefer `ISqlExecutor` for single round-trips, or `_connectionFactory.CreateCommand(sql, parameters, cancellationToken)` so timeout and cancellation stay aligned with `ISqlExecutor`.

## Naming

- Use explicit table/schema names (`dbo.Table`) in MERGE/UPSERT and cross-schema scripts to avoid ambiguity.
