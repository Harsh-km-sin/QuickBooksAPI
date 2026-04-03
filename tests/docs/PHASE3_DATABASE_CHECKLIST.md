# Phase 3 database checklist

Run against the **application** SQL database (same as API / `DefaultConnection`). See also `README.md` → **Database Setup for CFO Analytics & Intelligence**.

## Scripts (repo paths)

| Script | Creates |
|--------|---------|
| `Scripts/CreateForecastTables.sql` | `forecast_scenarios`, `forecast_results` |
| `Scripts/CreateCloseIssues.sql` | `close_issues` |
| `Scripts/CreateConsolidationTables.sql` | `dim_entity`, `fact_consolidated_pnl` |

## Verification queries (examples)

```sql
SELECT OBJECT_ID('dbo.forecast_scenarios', 'U'), OBJECT_ID('dbo.forecast_results', 'U');
SELECT OBJECT_ID('dbo.close_issues', 'U');
SELECT OBJECT_ID('dbo.dim_entity', 'U'), OBJECT_ID('dbo.fact_consolidated_pnl', 'U');
```

Expect non-`NULL` object ids after scripts succeed.

## Environments

- [ ] Local / dev
- [ ] Staging
- [ ] Production

Record date applied and operator in your runbook.

## Next: `dim_entity` data

After tables exist, seed entities for consolidation (see `Scripts/SeedDimEntity_Example.sql` and `README.md`).
