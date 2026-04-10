-- Phase 3 / CFO analytics table presence check (run against DefaultConnection database).
-- Example: sqlcmd -S localhost -d YourDb -E -i tests\scripts\verify-phase3-objects.sql
-- Expect non-NULL object ids for applied scripts. See tests/docs/PHASE3_DATABASE_CHECKLIST.md.

SELECT OBJECT_ID(N'dbo.forecast_scenarios', N'U') AS forecast_scenarios_id,
       OBJECT_ID(N'dbo.forecast_results', N'U') AS forecast_results_id;

SELECT OBJECT_ID(N'dbo.close_issues', N'U') AS close_issues_id;

SELECT OBJECT_ID(N'dbo.dim_entity', N'U') AS dim_entity_id,
       OBJECT_ID(N'dbo.fact_consolidated_pnl', N'U') AS fact_consolidated_pnl_id;
